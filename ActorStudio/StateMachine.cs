using FaceApiProxy;
using Microsoft.ProjectOxford.Face;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ActorStudio
{
    public class StateMachine : INotifyPropertyChanged
    {
        private State _currentState;
        private string _instructions;
        private ImageSource _recognizedFaceImage;
        private FaceServiceClient _faceClient;
        private const string _celebFacesGroupFolder = "Series";

        public event PropertyChangedEventHandler PropertyChanged;

        public FaceServiceClient FaceClient { get => _faceClient; set => _faceClient = value; }

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
                        case State.CheckingSmile:
                            Instructions = $"Ok, pour commencer,{Environment.NewLine}fais moi un sourire";
                            break;
                        case State.GameStarted:
                        case State.FaceRecognition:
                        case State.Idle:
                        case State.WaitingBigFace:
                        default:
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

        internal void Start()
        {
            CurrentState = State.WaitingBigFace;
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
            //CurrentState = State.GameStarted;
            CurrentState = State.Idle;
        }

        //private async void StartGameAsync()
        //{
        //var image = new BitmapImage();
        //image.SetSource(stream);

        //InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
        //ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
        //await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await MediaCapture.CapturePhotoToStreamAsync(imageProperties, stream));
        //var stream_send = stream.CloneStream().AsStream();
        //await FaceApiExtensions.CheckGroupAsync(_faceClient, stream_send);

        //var delayTask = Task.Delay(3000);
        //var tasks = new Task[] { faceTask, delayTask };
        //Task.WaitAll(tasks);
        //Instructions = $"Ah mais attends, on t'as déja dit que tu ressemblais à ...?";

        //Instructions = "Ok, on peut commencer à jouer...";

        //await Task.Delay(3000);
        //Instructions = $"Ah mais attends, on t'as déja dit que tu ressemblais à ...?";
        //}
    }

    public enum State
    {
        Idle,
        WaitingBigFace,
        CheckingSmile,
        FaceRecognition,
        GameStarted
    }
}
