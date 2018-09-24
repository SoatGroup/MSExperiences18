using System;
using Windows.UI.Xaml.Controls;

namespace ActorStudio
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FaceTrackingPage : Page
    {
        public FaceTrackingPage()
        {
            this.InitializeComponent();
            InitFaceTrackingAsync();
        }

        private async void InitFaceTrackingAsync()
        {
            try
            {
                // Start face detection
                await FaceTrackingControl.InitCameraAsync();
                FaceTrackingControl.StartFaceTracking();
                txtLocation.Text = "Face tracking started";
            }
            catch (Exception ex)
            {
                txtLocation.Text = "Unable to initialize camera for audio/video mode: " + ex.Message;
            }
        }
    }
}