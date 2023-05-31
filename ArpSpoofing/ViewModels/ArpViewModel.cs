using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Net;

namespace ArpSpoofing.ViewModels
{
    public partial class ArpViewModel : ObservableObject
    {
        [ObservableProperty]
        private string startScanIp;

        [ObservableProperty]
        private string endScanIp;

        public ArpViewModel()
        {
            var response = WeakReferenceMessenger.Default.Send<RequestMessage<string>, string>("RequestScanIp");
            if (response.HasReceivedResponse)
            {
                StartScanIp = response.Response;
            }

        }
    }
}