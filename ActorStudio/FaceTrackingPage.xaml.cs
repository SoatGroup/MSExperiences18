using FaceControls;
using Microsoft.ProjectOxford.Face;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Windows.Media.FaceAnalysis;
using Windows.Storage;
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
        private const string _celebFacesListId = "f03a14f5-65ff-43b1-be5e-36800680c303";
        private const string _celebFacesListName = "Series";
        private const string _celebFacesGroupFolder = "Series";

        private StorageFile photoFile;
        private readonly string PHOTO_FILE_NAME = "photo.jpg";

        private FaceServiceClient faceClient;
        //0 for false, 1 for true.
        private static int isCheckingSmile = 0;

        public StateMachine GameStateMachineVM { get; set; }

        public FaceTrackingPage()
        {
            this.InitializeComponent();
            this.DataContextChanged += (s, e) => Bindings.Update();
            GameStateMachineVM = this.DataContext as StateMachine;

            faceClient = new FaceServiceClient("34f95dfe9ef7460e9bfbd19987a5b6c3", "https://westeurope.api.cognitive.microsoft.com/face/v1.0");
            GameStateMachineVM.FaceClient = faceClient;
            FaceTrackingControl.FaceClient = faceClient;

            InitFaceTrackingAsync();
            GameStateMachineVM.Start();
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
                    FaceTrackingControl.IsCheckSmileEnabled = true;
                    FaceTrackingControl.SmileDetected += FaceTrackingControl_SmileDetected;
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => this.GameStateMachineVM.CurrentState = State.CheckingSmile);
                }
                else
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => GameStateMachineVM.Instructions = "Come closer !");
                }
            }
        }

        private async void FaceTrackingControl_SmileDetected(object sender, Microsoft.ProjectOxford.Face.Contract.Face args)
        {
            if (GameStateMachineVM.CurrentState == State.CheckingSmile && isCheckingSmile != 1)
            {
                // 0 indicates that the method is not in use.
                if (0 == Interlocked.Exchange(ref isCheckingSmile, 1))
                {
                    //store the captured image
                    var photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                    await FaceTrackingControl.GetCaptureFileAsync(photoFile);
                    var fileStream = await photoFile.OpenReadAsync();
                    var stream = fileStream.CloneStream().AsStream();
                    //var stream = await FaceTrackingControl.GetCaptureStreamAsync();
                    var match = await FaceDatasetHelper.CheckGroupAsync(faceClient, stream, _celebFacesListId, _celebFacesGroupFolder);
                    if (match == null)
                    {
                        //txtLocation.Text = "Response: No matching person.";
                    }
                    else
                    {
                        FaceTrackingControl.StopFaceTracking();
                        FaceTrackingControl.IsCheckSmileEnabled = false;
                        await GameStateMachineVM.StartFaceCompareAsync(match);
                    }
                    Interlocked.Exchange(ref isCheckingSmile, 0);
                }
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
                FaceTrackingControl.StartFaceTracking();
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