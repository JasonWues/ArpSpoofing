using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArpSpoofing.Entity
{
    public class Computer
    {
        public string IPAddress { get; set; }
        public string MacAddress { get; set; }
        public bool IsSelected { get; set; }
    }
}
