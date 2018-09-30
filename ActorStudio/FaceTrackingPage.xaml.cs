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

        public StateMachine GameStateMachineVM { get; set; }

        public FaceTrackingPage()
        {
            this.InitializeComponent();
            this.DataContextChanged += (s, e) => Bindings.Update();
            GameStateMachineVM = this.DataContext as StateMachine;
            faceClient = new FaceServiceClient("34f95dfe9ef7460e9bfbd19987a5b6c3", "https://westeurope.api.cognitive.microsoft.com/face/v1.0");

            InitFaceTrackingAsync();
            GameStateMachineVM.CurrentState = State.WaitingBigFace;
        }

        private async void FaceTrackingControl_FaceDetected(Windows.Media.Core.FaceDetectionEffect sender, Windows.Media.Core.FaceDetectedEventArgs args)
        {
            if (GameStateMachineVM.CurrentState == State.WaitingBigFace)
            {
                if (args.ResultFrame.DetectedFaces.Any() == false)
                {
                    return;
                }
                if (CheckBigFaces(args.ResultFrame.DetectedFaces))
                {
                    FaceTrackingControl.FaceDetected -= FaceTrackingControl_FaceDetected;
                    //FaceTrackingControl.IsCheckSmileEnabled = true;
                    //FaceTrackingControl.SmileDetected += FaceTrackingControl_SmileDetected;
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => this.GameStateMachineVM.CurrentState = State.CheckingSmile);
                }
                else
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => GameStateMachineVM.Instructions = "Come closer !");
                }
            }
        }

        private void FaceTrackingControl_SmileDetected(object sender, Microsoft.ProjectOxford.Face.Contract.Face args)
        {
            if (GameStateMachineVM.CurrentState == State.CheckingSmile)
            {
                GameStateMachineVM.CurrentState = State.GameStarted;
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
                .FirstOrDefault(f => f.FaceBox.Height * f.FaceBox.Width > 16000);
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
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => GameStateMachineVM.Instructions = "Face tracking started");
            }
            catch (Exception ex)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => GameStateMachineVM.Instructions = "Unable to initialize camera for audio/video mode: " + ex.Message);
            }
        }
    }
}