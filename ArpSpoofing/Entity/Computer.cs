using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArpSpoofing.Entity
{
    public class Computer 
    {
        public string IPAddress { get; set; }
        public string MacAddress { get; set; }
        public bool IsSelect { get; set; }

        public int Sort { get; set; }
    }
}
