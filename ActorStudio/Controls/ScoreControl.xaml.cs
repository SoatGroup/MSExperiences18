using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ActorStudio.Controls
{
    public sealed partial class ScoreControl : UserControl
    {
        public ScoreControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Contains the Text for the Score Control
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// Contains the Score for the Score Control
        /// </summary>
        public string Score
        {
            get { return (string)GetValue(ScoreProperty); }
            set { SetValue(ScoreProperty, value); }
        }

        /// <summary>
        /// Dependency Property to hold the Text for the Score Control.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ScoreControl), new PropertyMetadata(null, OnPropertyChanged));

        /// <summary>
        /// Dependency Property to hold the Score for the Score Control.
        /// </summary>
        public static readonly DependencyProperty ScoreProperty =
            DependencyProperty.Register("Score", typeof(string), typeof(ScoreControl), new PropertyMetadata(null, OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((string)e.NewValue != (string)e.OldValue)
            {
                //Debug.WriteLine(e.NewValue);
            }
        }
    }
}
