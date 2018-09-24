using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ActorStudio
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Navigate pages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((e.AddedItems[0] as ListViewItem).Tag.ToString().Equals("0"))
            {
                navSplitView.IsPaneOpen = !navSplitView.IsPaneOpen;
            }
            if ((e.AddedItems[0] as ListViewItem).Tag.ToString().Equals("1"))
            {
                frmPages.Navigate(typeof(FaceTrackingPage));
                navSplitView.IsPaneOpen = false;
            }
        }
    }
}