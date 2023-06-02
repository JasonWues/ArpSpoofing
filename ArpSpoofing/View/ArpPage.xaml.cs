using ArpSpoofing.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ArpSpoofing.View
{
    public sealed partial class ArpPage : Page
    {
        public ArpPage()
        {
            InitializeComponent();
            ViewModel = new ArpViewModel();
        }

        public ArpViewModel ViewModel { get; set; }

    }
}
