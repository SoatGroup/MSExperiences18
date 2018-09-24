using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Media.FaceAnalysis;
using Windows.UI;
using System.Collections.Generic;

namespace MSEmpotionAPI
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