using FaceApiProxy;
using Microsoft.ProjectOxford.Face;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.FaceAnalysis;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ActorStudio
{
    public class StateMachine : INotifyPropertyChanged
    {
        private const string key_face = "34f95dfe9ef7460e9bfbd19987a5b6c3";
        private const string face_apiroot = "https://westeurope.api.cognitive.microsoft.com/face/v1.0";
        private const string _celebFacesListId = "f03a14f5-65ff-43b1-be5e-36800680c303";
        private const string _celebFacesListName = "Series";
        private const string _celebFacesGroupFolder = "Series";
        private const int BigFaceSizeThreshold = 100000;
        private readonly string PHOTO_FILE_NAME = "photo.jpg";

        private State _currentState;
        private string _instructions;
        private ImageSource _recognizedFaceImage;
        private FaceServiceClient _faceClient;
        private CoreDispatcher _dispatcher;
        private FaceControls.FaceTrackingControl _faceTrackingControl;
        //0 for false, 1 for true.
        private static int isCheckingSmile = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Instructions
        {
            get => _instructions;
            set
            {
                _instructions = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Instructions)));
            }
        }

        public State CurrentState
        {
            get => _currentState;
            set
            {
                if (value != _currentState)
                {
                    _currentState = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentState)));
                    switch (value)
                    {
                        case State.FacesDetection:
                            // Start face detection
                            _faceTrackingControl.StartFaceTracking();
                            Instructions = null;
                            break;
                        case State.CheckingSmile:
                            _faceTrackingControl.IsCheckSmileEnabled = true;
                            Instructions = $"Ok, pour commencer,{Environment.NewLine}fais moi un sourire";
                            break;
                        case State.FaceRecognition:
                            _faceTrackingControl.StopFaceTracking();
                            _faceTrackingControl.IsCheckSmileEnabled = false;
                            break;
                        case State.GameStarted:
                            CurrentState = State.FacesDetection;
                            break;
                        case State.Idle:
                        default:
                            _faceTrackingControl.StopFaceTracking();
                            Instructions = null;
                            break;
                    }
                }
            }
        }

        public ImageSource RecognizedFaceImage
        {
            get => _recognizedFaceImage;
            set
            {
                _recognizedFaceImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecognizedFaceImage)));
            }
        }

        public StateMachine()
        {
            _faceClient = new FaceServiceClient("34f95dfe9ef7460e9bfbd19987a5b6c3", "https://westeurope.api.cognitive.microsoft.com/face/v1.0");
        }

        internal async void StartAsync(FaceControls.FaceTrackingControl faceTrackingControl, Windows.UI.Core.CoreDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _faceTrackingControl = faceTrackingControl;
            _faceTrackingControl.FaceClient = _faceClient;
            _faceTrackingControl.FaceDetected += FaceTrackingControl_FaceDetected;
            _faceTrackingControl.SmileDetected += FaceTrackingControl_SmileDetected;

            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    await _faceTrackingControl.InitCameraAsync();

                    CurrentState = State.FacesDetection;
                }
                catch (Exception ex)
                {
                    Instructions = "Unable to initialize camera for audio/video mode: " + ex.Message;
                }
            });
        }

        public async Task StartFaceCompareAsync(IdentifiedFace match)
        {
            CurrentState = State.FaceRecognition;
            using (var photoStream = await PicturesHelper.GetPersonPictureAsync(_celebFacesGroupFolder, match.PersonName))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.SetSource(photoStream);
                RecognizedFaceImage = bitmap;
            }
            Instructions = $"On t'a déja dit que{Environment.NewLine}tu ressemblais à{Environment.NewLine}{match.PersonName} ?";
            await Task.Delay(5000);
            RecognizedFaceImage = null;
            Instructions = null;
            await Task.Delay(1000);
            Instructions = $"Voyons si tu peux{Environment.NewLine}intégrer le casting de{Environment.NewLine}Game Of Thrones !";
            await Task.Delay(5000);
            CurrentState = State.GameStarted;
        }

        public async void FaceTrackingControl_FaceDetected(Windows.Media.Core.FaceDetectionEffect sender, Windows.Media.Core.FaceDetectedEventArgs args)
        {
            var areBigFacesDetected = CheckBigFaces(args.ResultFrame.DetectedFaces);
            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (CurrentState == State.FacesDetection)
                    {
                        // actually checking faces in screen
                        if (areBigFacesDetected)
                        {
                            // at least one 'big' face is detected so ew enable the smile check
                            CurrentState = State.CheckingSmile;
                        }
                        else if (args.ResultFrame.DetectedFaces.Any())
                        {
                            // at least one 'small' face is detected so we encourage the person to come closer
                            Instructions = $"N'aie pas peur...{Environment.NewLine}Approche toi !";
                        }
                        else
                        {
                            // nobody in front of the camera, we set another message
                            Instructions = null;
                        }
                    }
                    else if (CurrentState == State.CheckingSmile)
                    {
                        //actually checking smile one 'big' faces (with smileDetected event)
                        if (areBigFacesDetected == false)
                        {
                            // if no face was detected, return to state WaitingBigFace
                            // careful to multiple facetracking
                            CurrentState = State.FacesDetection;
                        }
                    }
                });
        }

        private async void FaceTrackingControl_SmileDetected(object sender, Microsoft.ProjectOxford.Face.Contract.Face args)
        {
            if (CurrentState == State.CheckingSmile && isCheckingSmile != 1)
            {
                await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    // 0 indicates that the method is not in use.
                    if (0 == Interlocked.Exchange(ref isCheckingSmile, 1))
                    {
                        //store the captured image
                        var photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                        await _faceTrackingControl.GetCaptureFileAsync(photoFile);
                        var fileStream = await photoFile.OpenReadAsync();
                        var stream = fileStream.CloneStream().AsStream();
                        //var stream = await FaceTrackingControl.GetCaptureStreamAsync();
                        var match = await FaceDatasetHelper.CheckGroupAsync(_faceClient, stream, _celebFacesListId, _celebFacesGroupFolder);
                        if (match == null)
                        {
                            //txtLocation.Text = "Response: No matching person.";
                        }
                        else
                        {
                            await StartFaceCompareAsync(match);
                        }
                        Interlocked.Exchange(ref isCheckingSmile, 0);
                    }
                });
            }
        }

        private bool CheckBigFaces(IReadOnlyList<DetectedFace> detectedFaces)
        {
            if (detectedFaces == null || !detectedFaces.Any())
            {
                return false;
            }

            var biggestFace = detectedFaces
                .OrderByDescending(f => f.FaceBox.Height * f.FaceBox.Width)
                .FirstOrDefault(f => f.FaceBox.Height * f.FaceBox.Width > BigFaceSizeThreshold);
            return biggestFace != null;
        }
    }
}
