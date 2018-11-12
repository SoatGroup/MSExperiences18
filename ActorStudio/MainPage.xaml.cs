using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

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
            InitializeComponent();
        }

        /// <summary>
        /// Navigate pages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ListViewItem) e.AddedItems[0]).Tag.ToString().Equals("0"))
            {
                navSplitView.IsPaneOpen = !navSplitView.IsPaneOpen;
            }
            if (((ListViewItem) e.AddedItems[0]).Tag.ToString().Equals("1"))
            {
                frmPages.Navigate(typeof(FaceTrackingPage));
                navSplitView.IsPaneOpen = false;
            }
        }

        private void TextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            navSplitView.IsPaneOpen = !navSplitView.IsPaneOpen;
        }
    }
}