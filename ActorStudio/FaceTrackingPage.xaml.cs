using System;
using System.Numerics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Display;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Toolkit.Uwp.Helpers;

namespace ActorStudio
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FaceTrackingPage : Page
    {
        PrintHelper _printHelper;

        public GameViewModel GameStateMachineVm { get; set; }

        public FaceTrackingPage()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
            GameStateMachineVm = DataContext as GameViewModel;

            var dispatcher = CoreApplication.GetCurrentView().CoreWindow.Dispatcher;
            GameStateMachineVm.StartAsync(FaceTrackingControl, dispatcher);

            GameStateMachineVm.ImageCaptured += GameStateMachineVM_ImageCaptured;
            GameStateMachineVm.AllEmotionsCaptured += GameStateMachineVM_AllEmotionsCaptured;
            
            // Connect Animation custom settings, the default animation is 0.8s with no easig and may need to be customized
            var compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            var connectedAnimationService = ConnectedAnimationService.GetForCurrentView();
            connectedAnimationService.DefaultDuration = TimeSpan.FromSeconds(1.0);
            connectedAnimationService.DefaultEasingFunction = compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.41f, 0.52f),
                new Vector2(0.00f, 0.94f)
            );
        }

        private void GameStateMachineVM_ImageCaptured(object sender, EventArgs e)
        {
            // Flash screen
            flashStoryboard.Begin();
        }

        private async void GameStateMachineVM_AllEmotionsCaptured(object sender, EventArgs e)
        {
            //Start animations
            var hapinessAnimation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("displayHapinessResultAnimation", UserHappinessImage);
            hapinessAnimation.TryStart(ResultControl.UserHappinessResultImage);

            var sadnessAnimation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("displaySadnessAnimation", UserSadnessImage);
            sadnessAnimation.TryStart(ResultControl.UserSadnessResultImage);

            var angerAnimation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("displayAngerResultAnimation", UserAngerImage);
            angerAnimation.TryStart(ResultControl.UserAngerResultImage);

            var surpriseAnimation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("displaySurpriseAnimation", UserSurpriseImage);
            surpriseAnimation.TryStart(ResultControl.UserSurpriseResultImage);

            // Wait for the animation to complete
            await Task.Delay(2000);

            await SaveResultImageAsync();
            await SendResultToPrinterAsync();
        }

        private async Task SendResultToPrinterAsync()
        {
            _printHelper = new PrintHelper(ResultControl.PhotoboothImageGrid);
            _printHelper.OnPrintCanceled += PrintHelper_OnPrintTaskFinished;
            _printHelper.OnPrintFailed += PrintHelper_OnPrintTaskFinished;
            _printHelper.OnPrintSucceeded += PrintHelper_OnPrintTaskFinished;
            //bool options = new ;
            await _printHelper.ShowPrintUIAsync("Soat - ActorStudio", true);
        }

        public async Task SaveResultImageAsync()
        {
            var rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(ResultControl);

            var pixelBuffer = await rtb.GetPixelsAsync();

            var displayInformation = DisplayInformation.GetForCurrentView();
            var file = await PicturesHelper.SaveResultBufferAsync(pixelBuffer, rtb.PixelWidth, rtb.PixelHeight, displayInformation.RawDpiX, displayInformation.RawDpiY);
            
            GameStateMachineVm.ResultImage = new BitmapImage(new Uri(file.Path));
        }

        private async void PrintHelper_OnPrintTaskFinished()
        {
            await GameStateMachineVm.EndGameAsync();
        }
    }
}