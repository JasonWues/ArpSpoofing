using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace ArpSpoofing.Entity
{
    public class ArpAttackComputer : ObservableObject
    {
        public bool Succeed { get; set; } //是否攻击成功

        public string IPAddress { get; set; }
        public string MacAddress { get; set; }
        public Task ArpAttackTask { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }

        public void SendArpSpoofing()
        {
            ArpAttackTask?.Start();
        }

        public void CancelTask()
        {
            CancellationTokenSource.Cancel();
        }

    }
}
