using System;
using Windows.UI.Xaml.Controls;

namespace ActorStudio
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FaceTrackingPage : Page
    {
        public StateMachine GameStateMachineVM { get; set; }

        public FaceTrackingPage()
        {
            this.InitializeComponent();
            this.DataContextChanged += (s, e) => Bindings.Update();
            GameStateMachineVM = this.DataContext as StateMachine;

            var dispatcher = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().CoreWindow.Dispatcher;
            GameStateMachineVM.StartAsync(FaceTrackingControl, dispatcher);
        }
    }
}