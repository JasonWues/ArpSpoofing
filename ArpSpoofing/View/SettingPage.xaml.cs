using ArpSpoofing.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ArpSpoofing.View
{

    public sealed partial class SettingPage : Page
    {
        public SettingPage()
        {
            InitializeComponent();
            ViewModel = new SettingViewModel();
        }

        public SettingViewModel ViewModel { get; set; }
    }
}
