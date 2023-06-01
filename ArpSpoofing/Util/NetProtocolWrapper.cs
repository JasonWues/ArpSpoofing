using PacketDotNet;
using System.Net;
using System.Net.NetworkInformation;

namespace ArpSpoofing.Util
{
    public class NetProtocolWrapper
    {
        public static Packet BuildArpRequest(IPAddress targetIp, IPAddress localIp, PhysicalAddress localMac)
        {
            var ethernetPacket = new EthernetPacket(localMac, PhysicalAddress.Parse("FF-FF-FF-FF-FF-FF"), EthernetType.Arp);
            var arpPacket = new ArpPacket(ArpOperation.Request, PhysicalAddress.Parse("00-00-00-00-00-00"), targetIp, localMac, localIp);
            ethernetPacket.PayloadPacket = arpPacket;

            return ethernetPacket;
        }
    }
}
