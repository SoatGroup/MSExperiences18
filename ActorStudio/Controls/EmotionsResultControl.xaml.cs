using Windows.UI.Xaml.Controls;

namespace ActorStudio.Controls
{
    public sealed partial class EmotionsResultControl : UserControl
    {
        public EmotionsResultControl()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                ViewModel = DataContext as GameViewModel;
            };
        }

        public GameViewModel ViewModel { get; set; }
    }
}
