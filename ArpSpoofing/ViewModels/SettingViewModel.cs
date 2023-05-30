using CommunityToolkit.Mvvm.ComponentModel;
using SharpPcap.LibPcap;
using System.Collections.Generic;
using System.Linq;

namespace ArpSpoofing.ViewModels
{
    public partial class SettingViewModel : ObservableObject
    {
        [ObservableProperty]
        private string selectNetCard;

        [ObservableProperty]
        private string localIP;

        [ObservableProperty]
        private string localMac;

        [ObservableProperty]
        private string gatewayIp;

        [ObservableProperty]
        private string gatewayMac;

        partial void OnSelectNetCardChanged(string value)
        {
            var selectNet = LibPcapLiveDeviceList.Instance.FirstOrDefault(x => x.Name == value);
            if (selectNet != null)
            {
                var localip = selectNet.Addresses.FirstOrDefault(x => x.Addr.type == Sockaddr.AddressTypes.AF_INET_AF_INET6
                                        && x.Addr.ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (localip != null)
                {
                    LocalIP = localip.Addr.ipAddress.ToString();
                }
                LocalMac = selectNet.MacAddress.ToString();

                GatewayMac = selectNet.Interface.GatewayAddresses.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();
            }

        }

        public static IReadOnlyCollection<string> GetLibPcapLiveDevices()
        {
            return LibPcapLiveDeviceList.Instance.Select(x => x.Name).ToList();
        }
    }
}