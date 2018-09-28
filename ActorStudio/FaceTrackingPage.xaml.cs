using FaceControls;
using Microsoft.ProjectOxford.Face;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Media.FaceAnalysis;
using Windows.UI.Xaml.Controls;

namespace ActorStudio
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FaceTrackingPage : Page
    {
        //Face API Key
        private const string key_face = "34f95dfe9ef7460e9bfbd19987a5b6c3";
        private const string face_apiroot = "https://westeurope.api.cognitive.microsoft.com/face/v1.0";

        private FaceServiceClient faceClient;
        private State _currentState;

        public FaceTrackingPage()
        {
            this.InitializeComponent();
            _currentState = State.Idle;
            faceClient = new FaceServiceClient("34f95dfe9ef7460e9bfbd19987a5b6c3", "https://westeurope.api.cognitive.microsoft.com/face/v1.0");

            InitFaceTrackingAsync();
            _currentState = State.WaitingBigFace;
        }

        private async void FaceTrackingControl_FaceDetected(Windows.Media.Core.FaceDetectionEffect sender, Windows.Media.Core.FaceDetectedEventArgs args)
        {
            switch (_currentState)
            {
                case State.Idle:
                    break;
                case State.WaitingBigFace:
                    if (CheckBigFaces(args.ResultFrame.DetectedFaces))
                    {
                        FaceTrackingControl.FaceDetected -= FaceTrackingControl_FaceDetected;
                        FaceTrackingControl.IsCheckSmileEnabled = true;
                        FaceTrackingControl.SmileDetected += FaceTrackingControl_SmileDetected;
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => txtLocation.Text = "Smile tracking started");
                        this._currentState = State.CheckingSmile;
                    }
                    break;
                case State.GameStarted:
                    break;
            }
        }

        private void FaceTrackingControl_SmileDetected(object sender, Microsoft.ProjectOxford.Face.Contract.Face args)
        {
            if (_currentState == State.CheckingSmile)
            {
                StartGame();
            }
        }

        private bool CheckBigFaces(IReadOnlyList<DetectedFace> detectedFaces)
        {
            if (detectedFaces == null)
            {
                return false;
            }

            var biggestFace = detectedFaces
                .OrderByDescending(f => f.FaceBox.Height * f.FaceBox.Width)
                .FirstOrDefault(f => f.FaceBox.Height * f.FaceBox.Width > 400);
            return biggestFace != null;
        }

        private async void InitFaceTrackingAsync()
        {
            try
            {
                // Start face detection
                await FaceTrackingControl.InitCameraAsync();
                FaceTrackingControl.StartFaceTracking(faceClient);
                FaceTrackingControl.FaceDetected += FaceTrackingControl_FaceDetected;
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => txtLocation.Text = "Face tracking started");
            }
            catch (Exception ex)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => txtLocation.Text = "Unable to initialize camera for audio/video mode: " + ex.Message);
            }
        }

        private void StartGame()
        {
            txtLocation.Text = "Starting Game";
        }

        private enum State
        {
            Idle,
            WaitingBigFace,
            CheckingSmile,
            GameStarted
        }
    }
}