using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using ArpSpoofing.View;

namespace ArpSpoofing
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SystemBackdrop = new MicaBackdrop();
        }

        private void MainNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if(args.SelectedItemContainer is NavigationViewItem item)
            {
                switch (item.Tag)
                {
                    case "Settings":
                        contentFrame.Navigate(typeof(SettingPage));
                        break;
                }
            }

        }
    }
}
