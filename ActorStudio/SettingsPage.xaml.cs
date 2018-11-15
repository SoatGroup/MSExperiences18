using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ActorStudio
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            this.DataContext = SettingsHelper.Instance;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this.cameraSourceComboBox.ItemsSource = await SettingsHelper.GetAvailableCameraNamesAsync();
            this.cameraSourceComboBox.SelectedItem = SettingsHelper.Instance.CameraName;
            base.OnNavigatedFrom(e);
        }

        private void OnGenerateNewKeyClicked(object sender, RoutedEventArgs e)
        {
            SettingsHelper.Instance.FaceListId = Guid.NewGuid().ToString();
        }

        private async void OnResetSettingsClick(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => SettingsHelper.Instance.RestoreAllSettings());
            await new MessageDialog("Settings restored. Please restart the application to load the default settings.").ShowAsync();
        }

        private void OnCameraSourceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.cameraSourceComboBox.SelectedItem != null)
            {
                SettingsHelper.Instance.CameraName = this.cameraSourceComboBox.SelectedItem.ToString();
            }
        }
    }
}
