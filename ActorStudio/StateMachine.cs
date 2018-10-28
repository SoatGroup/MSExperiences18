using FaceApiProxy;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.Toolkit.Uwp.Connectivity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
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
                            Instructions = $"Pour commencer à jouer{Environment.NewLine}fais moi un sourire";
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

        internal async Task EndGameAsync()
        {
            IsEmotionsResultsVisible = false;
            IsEmotionsCaptureVisible = false;

            UserHappinessImage = null;
            UserSadnessImage = null;
            UserAngerImage = null;
            UserSurpriseImage = null;

            ResultImage = null;

            // Clean all Files
            await PicturesHelper.CleanFaceCapturesAsync();

            await Task.Delay(3000);

            this.CurrentState = State.Idle;
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

        #region User Captured Images and Scores

        private ImageSource _userHappinessImage;
        public ImageSource UserHappinessImage
        {
            get => _userHappinessImage;
            set
            {
                _userHappinessImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserHappinessImage)));
            }
        }

        private ImageSource _userSadnessImage;
        public ImageSource UserSadnessImage
        {
            get => _userSadnessImage;
            set
            {
                _userSadnessImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserSadnessImage)));
            }
        }

        private ImageSource _userAngerImage;
        public ImageSource UserAngerImage
        {
            get => _userAngerImage;
            set
            {
                _userAngerImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserAngerImage)));
            }
        }

        private ImageSource _userSurpriseImage;
        public ImageSource UserSurpriseImage
        {
            get => _userSurpriseImage;
            set
            {
                _userSurpriseImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserSurpriseImage)));
            }
        }

        private string _hapinessScore;
        public string HapinessScore
        {
            get => _hapinessScore;
            set
            {
                _hapinessScore = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HapinessScore)));
            }
        }

        private string _surpriseScore;
        public string SupriseScore
        {
            get => _surpriseScore;
            set
            {
                _surpriseScore = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SupriseScore)));
            }
        }

        private string _sadnessScore;
        public string SadnessScore
        {
            get => _sadnessScore;
            set
            {
                _sadnessScore = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SadnessScore)));
            }
        }

        private string _angerScore;
        public string AngerScore
        {
            get => _angerScore;
            set
            {
                _angerScore = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AngerScore)));
            }
        }

        private ImageSource _resultImage;
        public ImageSource ResultImage
        {
            get => _resultImage;
            set
            {
                _resultImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResultImage)));
            }
        }

        #endregion User Captured Images and Scores

        private Timer _timer;
        private const int _emotionCaptureDelayInSeconds = 3;
        private int _invokeTimerCount = 0;
        private string _timerDisplay;

        public string TimerDisplay
        {
            get => _timerDisplay;
            set
            {
                _timerDisplay = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimerDisplay)));
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

        public async Task StartFaceRecognizedAsync(IdentifiedFace match)
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
            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
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
                        else if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable == false)
                        {
                            var biggestFace = args.ResultFrame.DetectedFaces.OrderByDescending(f => f.FaceBox.Height * f.FaceBox.Width).First();
                            await StartFaceRecognitionAsync(biggestFace.FaceBox);
                        }
                    }
                });
        }

        private async void FaceTrackingControl_SmileDetected(object sender, Microsoft.ProjectOxford.Face.Contract.Face face)
        {
            if (CurrentState == State.CheckingSmile && isCheckingSmile != 1)
            {
                var facebox = new BitmapBounds()
                {
                    Height = (uint)face.FaceRectangle.Height,
                    Width = (uint)face.FaceRectangle.Width,
                    X = (uint)face.FaceRectangle.Left,
                    Y = (uint)face.FaceRectangle.Top
                };
                await StartFaceRecognitionAsync(facebox);
            }
        }

        private async Task StartFaceRecognitionAsync(BitmapBounds facebox)
        {
            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                ImageCaptured?.Invoke(this, null);

                CurrentState = State.FaceRecognition;

                // 0 indicates that the method is not in use.
                if (0 == Interlocked.Exchange(ref isCheckingSmile, 1))
                {
                    if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    {
                        //store the captured image
                        var photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(Constants.PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                        await _faceTrackingControl.CaptureFaceToFileAsync(photoFile, facebox);
                        var fileStream = await photoFile.OpenReadAsync();
                        var streamCompare = fileStream.CloneStream();
                        BitmapImage originalBitmap = new BitmapImage();
                        originalBitmap.SetSource(streamCompare);
                        OriginalFaceImage = originalBitmap;

                        IsFaceMatchingRunning = true;

                        var streamCheck = fileStream.CloneStream().AsStream();
                        var match = await FaceDatasetHelper.CheckGroupAsync(_faceClient, streamCheck, Constants._celebFacesListId, Constants._celebFacesGroupFolder);
                        await StartFaceRecognizedAsync(match);
                    }
                    else
                    {
                        await StartFaceRecognizedAsync(null);
                    }
                    Interlocked.Exchange(ref isCheckingSmile, 0);
                }
                IsFaceMatchingRunning = false;
            });
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

            HapinessScore = null;
            SadnessScore = null;
            SupriseScore = null;
            AngerScore = null;
            IsEmotionsCaptureVisible = true;

            Instructions = $"Tu vas devoir nous jouer {Environment.NewLine} plusieurs émotions {Environment.NewLine} Prêt ?";
            await Task.Delay(waitMillisecondsDelay);

            var captureTimerDelay = TimeSpan.FromSeconds(_emotionCaptureDelayInSeconds);

            await WaitAndCaptureEmotionAsync("LA JOIE", captureTimerDelay, Emotion.Hapiness);

            await WaitAndCaptureEmotionAsync("LA TRISTESSE", captureTimerDelay, Emotion.Sadness);

            await WaitAndCaptureEmotionAsync("LA COLÈRE", captureTimerDelay, Emotion.Anger);

            await WaitAndCaptureEmotionAsync("LA SURPRISE", captureTimerDelay, Emotion.Surprise);

            await Task.Delay(2000);

            CurrentState = State.GameEnded;
        }

        private async Task WaitAndCaptureEmotionAsync(string emotionName, TimeSpan waitDelay, Emotion emotion)
        {
            Instructions = null;
            await Task.Delay(1000);
            Instructions = $"- {emotionName.ToUpper()} -";
            StartCaptureTimer();
            await Task.Delay(waitDelay);
            // Capture image
            ImageCaptured?.Invoke(this, null);
            // Store the captured image
            BitmapImage originalBitmap = new BitmapImage();

            string filePath;

            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                StorageFile photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(Constants.PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                await _faceTrackingControl.CaptureFaceToFileAsync(photoFile);
                var fileStream = await photoFile.OpenReadAsync();
                var imageStream = fileStream.CloneStream();
                var scoreStream = fileStream.CloneStream();

                originalBitmap.SetSource(imageStream);
                filePath = photoFile.Path;

                switch (emotion)
                {
                    case Emotion.Hapiness:
                        UserHappinessImage = originalBitmap;
                        break;
                    case Emotion.Surprise:
                        UserSurpriseImage = originalBitmap;
                        break;
                    case Emotion.Sadness:
                        UserSadnessImage = originalBitmap;
                        break;
                    case Emotion.Anger:
                        UserAngerImage = originalBitmap;
                        break;
                    default:
                        break;
                }

                var emotionScore = await DetectEmotionAsync(scoreStream);
                if (emotionScore != null)
                {
                    switch (emotion)
                    {
                        case Emotion.Hapiness:
                            HapinessScore = $"{Math.Round(emotionScore.Happiness * 100, 0)}%";
                            break;
                        case Emotion.Surprise:
                            SupriseScore = $"{Math.Round(emotionScore.Surprise * 100, 0)}%";
                            break;
                        case Emotion.Sadness:
                            SadnessScore = $"{Math.Round(emotionScore.Sadness * 100, 0)}%";
                            break;
                        case Emotion.Anger:
                            AngerScore = $"{Math.Round(emotionScore.Anger * 100, 0)}%";
                            break;
                        default:
                            break;
                    }
                }
            });
            Instructions = null;
        }

        private async Task<EmotionScores> DetectEmotionAsync(IRandomAccessStream photoStream)
        {
            try
            {
                var requiredFaceAttributes = new FaceAttributeType[] { FaceAttributeType.Emotion };
                var attributes = await FaceApiHelper.DetectEmotionsAsync(_faceClient, photoStream.AsStream(), requiredFaceAttributes);
                return attributes?.Emotion;
            }
            catch (Exception e)
            {
                // Hide exception and return null
                return null;
            }
        }

        private async void DisplayResultsAsync()
        {
            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                IsEmotionsResultsVisible = true;
                AllEmotionsCaptured?.Invoke(this, null);
                IsEmotionsCaptureVisible = false;
                this.CurrentState = State.WaitForPrint;
            });
        }

        private void StartCaptureTimer()
        {
            _invokeTimerCount = 0;
            _timer = new Timer(TimerCallBack, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
        }

        private async void TimerCallBack(object state)
        {
            if (_invokeTimerCount >= _emotionCaptureDelayInSeconds)
            {
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { TimerDisplay = null; });
                _timer.Dispose();
            }
            else
            {
                var remainingSeconds = _emotionCaptureDelayInSeconds - _invokeTimerCount;
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { TimerDisplay = remainingSeconds.ToString(); });
                _invokeTimerCount++;
            }
        }

        private enum Emotion
        {
            Hapiness,
            Surprise,
            Sadness,
            Anger
        }
    }
}
