using Windows.UI.Xaml.Controls;

namespace ActorStudio.Controls
{
    public sealed partial class EmotionsResultControl : UserControl
    {
        public EmotionsResultControl()
        {
            this.InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                ViewModel = DataContext as StateMachine;
            };
        }

        public StateMachine ViewModel { get; set; }
    }
}
