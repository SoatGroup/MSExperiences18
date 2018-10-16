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
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ActorStudio
{
    public class StateMachine : INotifyPropertyChanged
    {
        private State _currentState;
        private string _instructions;
        private string _confidence;
        private bool _isFaceMatchVisible;
        private bool _isEmotionsCaptureVisible;
        private bool _isEmotionsResultsVisible;
        private bool _isFaceMatchingRunning;
        private FaceServiceClient _faceClient;
        private CoreDispatcher _dispatcher;
        private FaceControls.FaceTrackingControl _faceTrackingControl;
        //0 for false, 1 for true.
        private static int isCheckingSmile = 0;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler ImageCaptured;
        public event EventHandler AllEmotionsCaptured;

        public string Instructions
        {
            get => _instructions;
            set
            {
                _instructions = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Instructions)));
            }
        }

        public string Confidence
        {
            get => _confidence;
            set
            {
                _confidence = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Confidence)));
            }
        }

        public bool IsFaceMatchVisible
        {
            get => _isFaceMatchVisible;
            set
            {
                _isFaceMatchVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFaceMatchVisible)));
            }
        }

        public bool IsFaceMatchingRunning
        {
            get => _isFaceMatchingRunning;
            set
            {
                _isFaceMatchingRunning = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFaceMatchingRunning)));
            }
        }

        public bool IsEmotionsCaptureVisible
        {
            get => _isEmotionsCaptureVisible;
            set
            {
                _isEmotionsCaptureVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEmotionsCaptureVisible)));
            }
        }

        public bool IsEmotionsResultsVisible
        {
            get => _isEmotionsResultsVisible;
            set
            {
                _isEmotionsResultsVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEmotionsResultsVisible)));
            }
        }

        public State CurrentState
        {
            get => _currentState;
            set
            {
                if (value != _currentState)
                {
                    if (_currentState == value)
                        return;
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
                        case State.EmotionsCaptures:
                            _faceTrackingControl.StopFaceTracking();
                            _faceTrackingControl.IsCheckSmileEnabled = false;
                            StartCaptureEmotionsAsync();
                            break;
                        case State.GameEnded:
                            DisplayResultsAsync();
                            break;
                        case State.Idle:
                            CurrentState = State.FacesDetection;
                            break;
                        default:
                            _faceTrackingControl.StopFaceTracking();
                            Instructions = null;
                            break;
                    }
                }
            }
        }

        #region Face Matching Images

        private ImageSource _recognizedFaceImage;
        private ImageSource _originalFaceImage;

        public ImageSource RecognizedFaceImage
        {
            get => _recognizedFaceImage;
            set
            {
                _recognizedFaceImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecognizedFaceImage)));
            }
        }

        public ImageSource OriginalFaceImage
        {
            get => _originalFaceImage;
            set
            {
                _originalFaceImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OriginalFaceImage)));
            }
        }

        #endregion Face Matching Images

        #region Face Matching Images

        private ImageSource _userHappinessImage;
        private ImageSource _userSadnessImage;
        private ImageSource _userAngerImage;
        private ImageSource _userSurpriseImage;

        public ImageSource UserHappinessImage
        {
            get => _userHappinessImage;
            set
            {
                _userHappinessImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserHappinessImage)));
            }
        }

        public ImageSource UserSadnessImage
        {
            get => _userSadnessImage;
            set
            {
                _userSadnessImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserSadnessImage)));
            }
        }

        public ImageSource UserAngerImage
        {
            get => _userAngerImage;
            set
            {
                _userAngerImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserAngerImage)));
            }
        }

        public ImageSource UserSurpriseImage
        {
            get => _userSurpriseImage;
            set
            {
                _userSurpriseImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserSurpriseImage)));
            }
        }

        #endregion region User Captured Images

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

        public async Task StartFaceRecognizedAsync(IRandomAccessStream originalStream, IdentifiedFace match)
        {
            if (match == null)
            {
                Instructions = $"On t'as déja dit que{Environment.NewLine}tu ressemblais à{Environment.NewLine}Alain... De Loin ?";
                await Task.Delay(3000);
            }
            else
            {
                using (var photoStream = await PicturesHelper.GetPersonPictureAsync(Constants._celebFacesGroupFolder, match.PersonName))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.SetSource(photoStream);
                    RecognizedFaceImage = bitmap;
                }

                Instructions = $"On t'a déja dit que{Environment.NewLine}tu ressemblais à{Environment.NewLine}{match.PersonName} ?";
                Confidence = (match.Confidence * 100).ToString("N0") + "%";
                IsFaceMatchVisible = true;

                await Task.Delay(5000);
                IsFaceMatchVisible = false;
                RecognizedFaceImage = null;
            }

            OriginalFaceImage = null;
            Confidence = null;
            Instructions = null;
            await Task.Delay(1000);

            Instructions = $"Voyons si tu peux{Environment.NewLine}intégrer le casting de{Environment.NewLine}Game Of Thrones !";
            await Task.Delay(5000);

            CurrentState = State.EmotionsCaptures;
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
                    ImageCaptured?.Invoke(this, null);

                    CurrentState = State.FaceRecognition;

                    // 0 indicates that the method is not in use.
                    if (0 == Interlocked.Exchange(ref isCheckingSmile, 1))
                    {
                        //store the captured image
                        var photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(Constants.PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                        await _faceTrackingControl.CaptureFaceToFileAsync(photoFile, args.FaceRectangle);
                        var fileStream = await photoFile.OpenReadAsync();
                        var streamCompare = fileStream.CloneStream();
                        BitmapImage originalBitmap = new BitmapImage();
                        originalBitmap.SetSource(streamCompare);
                        OriginalFaceImage = originalBitmap;

                        IsFaceMatchingRunning = true;

                        var streamCheck = fileStream.CloneStream().AsStream();
                        var match = await FaceDatasetHelper.CheckGroupAsync(_faceClient, streamCheck, Constants._celebFacesListId, Constants._celebFacesGroupFolder);
                        await StartFaceRecognizedAsync(streamCompare, match);
                        Interlocked.Exchange(ref isCheckingSmile, 0);
                    }
                    IsFaceMatchingRunning = false;
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
                .FirstOrDefault(f => f.FaceBox.Height * f.FaceBox.Width > Constants.BigFaceSizeThreshold);
            return biggestFace != null;
        }

        private async void StartCaptureEmotionsAsync()
        {
            int waitMillisecondsDelay = 3000;

            IsEmotionsCaptureVisible = true;

            Instructions = $"Tu vas devoir nous jouer {Environment.NewLine} plusieurs émotions {Environment.NewLine} Prêt ?";
            await Task.Delay(waitMillisecondsDelay);

            UserHappinessImage = await WaitAndCaptureEmotionAsync("LA JOIE", UserHappinessImage, waitMillisecondsDelay);
            UserSadnessImage = await WaitAndCaptureEmotionAsync("LA TRISTESSE", UserSadnessImage, waitMillisecondsDelay);
            UserAngerImage = await WaitAndCaptureEmotionAsync("LA COLÈRE", UserAngerImage, waitMillisecondsDelay);
            UserSurpriseImage = await WaitAndCaptureEmotionAsync("LA SURPRISE", UserSurpriseImage, waitMillisecondsDelay);

            await Task.Delay(2000);

            CurrentState = State.GameEnded;
        }

        private async Task<ImageSource> WaitAndCaptureEmotionAsync(string emotionName, ImageSource image, int waitMillisecondsDelay)
        {
            Instructions = null;
            await Task.Delay(1000);
            Instructions = $"- {emotionName.ToUpper()} -";
            await Task.Delay(waitMillisecondsDelay);
            // Capture image
            ImageCaptured?.Invoke(this, null);
            // Store the captured image
            BitmapImage originalBitmap = new BitmapImage();
            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                var photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(Constants.PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                await _faceTrackingControl.CaptureFaceToFileAsync(photoFile);
                var fileStream = await photoFile.OpenReadAsync();
                var streamCompare = fileStream.CloneStream();
                originalBitmap.SetSource(streamCompare);
            });
            Instructions = null;
            return originalBitmap;
        }

        private async void DisplayResultsAsync()
        {
            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                IsEmotionsResultsVisible = true;
                AllEmotionsCaptured?.Invoke(this, null);
                IsEmotionsCaptureVisible = false;
                await Task.Delay(15000);

                IsEmotionsResultsVisible = false;
                IsEmotionsCaptureVisible = false;
                await Task.Delay(3000);

                UserHappinessImage = null;
                UserSadnessImage = null;
                UserAngerImage = null;
                UserSurpriseImage = null;
                this.CurrentState = State.Idle;
            });
        }
    }
}
