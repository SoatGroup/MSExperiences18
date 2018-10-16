using System;
using System.Numerics;
using Windows.UI.Composition;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;

namespace ActorStudio
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FaceTrackingPage : Page
    {
        private Compositor _compositor;

        public StateMachine GameStateMachineVM { get; set; }

        public FaceTrackingPage()
        {
            this.InitializeComponent();
            this.DataContextChanged += (s, e) => Bindings.Update();
            GameStateMachineVM = this.DataContext as StateMachine;

            var dispatcher = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().CoreWindow.Dispatcher;
            GameStateMachineVM.StartAsync(FaceTrackingControl, dispatcher);

            GameStateMachineVM.ImageCaptured += GameStateMachineVM_ImageCaptured;
            GameStateMachineVM.AllEmotionsCaptured += GameStateMachineVM_AllEmotionsCaptured;


            // Connect Animation custom settings, the default animation is 0.8s with no easig and may need to be customized
            this._compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            var connectedAnimationService = ConnectedAnimationService.GetForCurrentView();
            connectedAnimationService.DefaultDuration = TimeSpan.FromSeconds(1.0);
            connectedAnimationService.DefaultEasingFunction = this._compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.41f, 0.52f),
                new Vector2(0.00f, 0.94f)
            );
        }

        private void GameStateMachineVM_ImageCaptured(object sender, EventArgs e)
        {
            // Flash screen
            flashStoryboard.Begin();
        }

        private void GameStateMachineVM_AllEmotionsCaptured(object sender, EventArgs e)
        {
            var hapinessAnimation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("displayHapinessResultAnimation", UserHappinessImage);
            hapinessAnimation.TryStart(UserHappinessResultImage);

            var sadnessAnimation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("displaySadnessAnimation", UserSadnessImage);
            sadnessAnimation.TryStart(UserSadnessResultImage);

            var angerAnimation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("displayAngerResultAnimation", UserAngerImage);
            angerAnimation.TryStart(UserAngerResultImage);

            var surpriseAnimation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("displaySurpriseAnimation", UserSurpriseImage);
            surpriseAnimation.TryStart(UserSurpriseResultImage);
        }
    }
}