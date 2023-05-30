﻿using CommunityToolkit.Mvvm.ComponentModel;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace ArpSpoofing.ViewModels
{
    public partial class SettingViewModel : ObservableObject
    {
        private readonly TimeSpan _timeout = new(0, 0, 2);

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
            var request = BuildArpRequest();
            string arpFilter = "arp and ether dst " + LocalMac.ToString();
            selectNetCard.Open(DeviceModes.Promiscuous, 20);
            selectNetCard.Filter = arpFilter;
            var lastRequestTime = DateTimeOffset.MinValue;
            var requestInterval = TimeSpan.FromMilliseconds(200);

            ArpPacket arpPacket = null;
            var timeoutDateTime = DateTimeOffset.Now + _timeout;

            while(DateTimeOffset.Now < timeoutDateTime)
            {
                if (requestInterval < (DateTimeOffset.Now - lastRequestTime))
                {
                    selectNetCard.SendPacket(request);
                    lastRequestTime = DateTimeOffset.Now;
                }

                if(selectNetCard.GetNextPacket(out var packet) > 0)
                {
                    if(packet.Device.LinkType != LinkLayers.Ethernet)
                    {
                        continue;
                    }

                    var pack = Packet.ParsePacket(packet.Device.LinkType,packet.Data.ToArray());
                    arpPacket = pack.Extract<ArpPacket>();
                    if(arpPacket == null )
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

        private Packet BuildArpRequest()
        {
            var ethernetPacket = new EthernetPacket(LocalMac, PhysicalAddress.Parse("FF-FF-FF-FF-FF-FF"), EthernetType.Arp);
            var arpPacket = new ArpPacket(ArpOperation.Request, PhysicalAddress.Parse("00-00-00-00-00-00"), GatewayIp, LocalMac, LocalIP);
            ethernetPacket.PayloadPacket = arpPacket;

            return ethernetPacket;
        }

        public static IReadOnlyCollection<string> GetLibPcapLiveDevices()
        {
            return LibPcapLiveDeviceList.Instance.Select(x => x.Interface.FriendlyName).ToList();
        }
    }
}