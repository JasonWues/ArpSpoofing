using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml.Controls;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace ArpSpoofing.ViewModels
{
    public partial class SettingViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private string selectNetStr;

        private LibPcapLiveDevice selectNetCard;

        [ObservableProperty]
        private IPAddress localIP;

        [ObservableProperty]
        private PhysicalAddress localMac;

        [ObservableProperty]
        private IPAddress gatewayIp;

        [ObservableProperty]
        private PhysicalAddress gatewayMac;

        [RelayCommand]
        private async Task Load()
        {
            if (LibPcapLiveDeviceList.Instance.Count < 0)
            {
                ContentDialog dialog = new();

                dialog.Title = "错误";
                dialog.Content = "网卡数量不足";
                dialog.XamlRoot = App.MainRoot.XamlRoot;

                await dialog.ShowAsync();
            }
        }

        public SettingViewModel()
        {
            IsActive = true;
        }

        protected override void OnActivated()
        {
            WeakReferenceMessenger.Default.Register<SettingViewModel, RequestMessage<LibPcapLiveDevice>, string>(this, "RequestScanIp", (v, r) =>
            {
                r.Reply(v?.selectNetCard);
            });
        }

        partial void OnSelectNetStrChanged(string value)
        {
            selectNetCard = LibPcapLiveDeviceList.Instance.FirstOrDefault(x => x.Interface.FriendlyName == value);
            if (selectNetCard != null)
            {
                var localip = selectNetCard.Addresses.FirstOrDefault(x => x.Addr.type == Sockaddr.AddressTypes.AF_INET_AF_INET6
                                        && x.Addr.ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (localip != null)
                {
                    LocalIP = localip.Addr.ipAddress;
                }
                LocalMac = selectNetCard.MacAddress;

                var gatewayIp = selectNetCard.Interface.GatewayAddresses.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (gatewayIp == null)
                {
                    GatewayIp = null;
                    return;
                }
                GatewayIp = gatewayIp;

                GatewayMac = GetGatewayMac();


            }
        }

        private PhysicalAddress GetGatewayMac()
        {
            var request = Util.NetProtocolWrapper.BuildArpRequest(GatewayIp, LocalIP, LocalMac);
            string arpFilter = "arp and ether dst " + LocalMac.ToString();
            selectNetCard.Open(DeviceModes.Promiscuous, 20);
            selectNetCard.Filter = arpFilter;
            var lastRequestTime = DateTimeOffset.MinValue;
            var requestInterval = TimeSpan.FromMilliseconds(200);

            ArpPacket arpPacket = null;
            var timeoutDateTime = DateTimeOffset.Now + new TimeSpan(0, 0, 2);

            while (DateTimeOffset.Now < timeoutDateTime)
            {
                if (requestInterval < (DateTimeOffset.Now - lastRequestTime))
                {
                    selectNetCard.SendPacket(request);
                    lastRequestTime = DateTimeOffset.Now;
                }

                if (selectNetCard.GetNextPacket(out var packet) > 0)
                {
                    if (packet.Device.LinkType != LinkLayers.Ethernet)
                    {
                        continue;
                    }

                    var pack = Packet.ParsePacket(packet.Device.LinkType, packet.Data.ToArray());
                    arpPacket = pack.Extract<ArpPacket>();
                    if (arpPacket == null)
                    {
                        continue;
                    }

                    if (arpPacket.SenderProtocolAddress.Equals(GatewayIp))
                    {
                        break;
                    }
                }
            }

            selectNetCard.Close();
            return arpPacket?.SenderHardwareAddress;
        }


        public IReadOnlyCollection<string> GetLibPcapLiveDevices()
        {
            return LibPcapLiveDeviceList.Instance.Select(x => x.Interface.FriendlyName).ToList();
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