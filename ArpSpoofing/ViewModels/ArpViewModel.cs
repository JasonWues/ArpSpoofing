using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Net;

namespace ArpSpoofing.ViewModels
{
    public partial class ArpViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private string startScanIp;

        [ObservableProperty]
        private string endScanIp;

        public ArpViewModel()
        {
            IsActive = true;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            WeakReferenceMessenger.Default.Register<IPAddress, string>(this, "NetCardChange", (v, message) =>
            {
                var z = message.ToString();
                StartScanIp = z;
            });
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            WeakReferenceMessenger.Default.Unregister<ArpViewModel>(this);
        }
    }
}