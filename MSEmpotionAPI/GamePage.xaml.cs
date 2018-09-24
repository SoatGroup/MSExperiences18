using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MSEmpotionAPI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GamePage : Page
    {
        //Face API Key
        string key_face = "34f95dfe9ef7460e9bfbd19987a5b6c3";
        string face_apiroot = "https://westeurope.api.cognitive.microsoft.com/face/v1.0";
        Face[] faces;

        private int _startTime;
        private int _seconds = 5;
        private DispatcherTimer _timer = new DispatcherTimer();

        private MediaCapture mediaCapture;
        private StorageFile photoFile;
        private readonly string PHOTO_FILE_NAME = "photo.jpg";

        public GamePage()
        {
            this.InitializeComponent();
            initCamera();
        }

        private async void initCamera()
        {
            try
            {
                txtLocation.Text = "Initializing camera to capture audio and video...";
                // Use default initialization
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();

                // Set callbacks for failure and recording limit exceeded
                txtLocation.Text = "Device successfully initialized for video recording!";
                mediaCapture.Failed += new MediaCaptureFailedEventHandler(mediaCapture_Failed);
                // Start Preview                
                previewElement.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                txtLocation.Text = "Camera preview succeeded";
            }
            catch (Exception ex)
            {
                txtLocation.Text = "Unable to initialize camera for audio/video mode: " + ex.Message;
            }
        }

        private async void mediaCapture_Failed(MediaCapture currentCaptureObject, MediaCaptureFailedEventArgs currentFailure)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    txtLocation.Text = "MediaCaptureFailed: " + currentFailure.Message;
                }
                catch (Exception)
                {
                }
                finally
                {
                    txtLocation.Text += "\nCheck if camera is diconnected. Try re-launching the app";
                }
            });
        }
        private void StartTimer()
        {
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
            _startTime = Environment.TickCount;
            _timer.Start();
        }
        private async void Timer_Tick(object sender, object e)
        {
            var remainingSeconds = _seconds - TimeSpan.FromMilliseconds(Environment.TickCount - _startTime).Seconds;

            if (remainingSeconds <= 0)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                timerText.Text = String.Empty;
                // Capture Image
                await TakePhotoAsync();
            }
            else
            {
                timerText.Text = remainingSeconds.ToString();
            }

        }

        private void StartGame(object sender, RoutedEventArgs e)
        {
            StartTimer();
        }

        private async Task TakePhotoAsync()
        {
            try
            {
                // Store the captured image
                photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                    PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);
                txtLocation.Text = "Take Photo succeeded: " + photoFile.Path;
                // Flash screen
                flashStoryboard.Begin();
                // Compute image
                await ComputeStream(photoFile);
            }
            catch (Exception ex)
            {
                txtLocation.Text = ex.Message;
            }
        }

        async Task ComputeStream(StorageFile photoFile)
        {
            try
            {
                var stream = await photoFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
                var stream_send = stream.CloneStream();

                ringLoading.IsActive = true;

                //Face service
                FaceServiceClient f_client = new FaceServiceClient(key_face, face_apiroot);

                var requiedFaceAttributes = new FaceAttributeType[] {
                                FaceAttributeType.Age,
                                FaceAttributeType.Gender,
                                FaceAttributeType.Smile,
                                FaceAttributeType.FacialHair,
                                FaceAttributeType.HeadPose,
                                FaceAttributeType.Emotion,
                                FaceAttributeType.Glasses
                                };
                var faces_task = f_client.DetectAsync(stream_send.AsStream(), true, true, requiedFaceAttributes);

                faces = await faces_task;

                if (faces != null)
                {
                    DisplayEmotionsData(faces);
                }

                ringLoading.IsActive = false;
            }
            catch (Exception ex)
            {
                //lblError.Text = ex.Message;
                //lblError.Visibility = Visibility.Visible;
            }
        }

        private async void Cleanup()
        {
            if (mediaCapture != null)
            {
                // Cleanup MediaCapture object
                await mediaCapture.StopPreviewAsync();
                mediaCapture.Dispose();
                mediaCapture = null;
            }
        }

        /// <summary>
        /// Display Emotions data
        /// </summary>
        /// <param name="emotions"></param>
        private void DisplayEmotionsData(Face[] faces, bool init = true)
        {
            if (faces == null)
                return;
            if (!init)
                return;

            var list_child = gridEmotions.Children.ToList();
            list_child.ForEach((e) =>
            {
                if (e as TextBlock != null && (e as TextBlock).Tag != null)
                {
                    gridEmotions.Children.Remove(e);
                }
            });

            int index = 1;
            foreach (var face in faces)
            {
                TextBlock txt0 = new TextBlock();
                txt0.Padding = new Thickness(1);
                txt0.FontSize = 11;
                txt0.Text = "#" + index;
                Grid.SetRow(txt0, index + 1);
                Grid.SetColumn(txt0, 0);
                txt0.Tag = true;

                TextBlock txt1 = new TextBlock();
                txt1.Padding = new Thickness(1);
                txt1.FontSize = 11;
                txt1.Text = Math.Round(face.FaceAttributes.Emotion.Anger, 2).ToString();
                Grid.SetRow(txt1, index + 1);
                Grid.SetColumn(txt1, 1);
                txt1.Tag = true;

                TextBlock txt2 = new TextBlock();
                txt2.Padding = new Thickness(1);
                txt2.FontSize = 11;
                txt2.Text = Math.Round(face.FaceAttributes.Emotion.Contempt, 2).ToString();
                Grid.SetRow(txt2, index + 1);
                Grid.SetColumn(txt2, 2);
                txt2.Tag = true;

                TextBlock txt3 = new TextBlock();
                txt3.Padding = new Thickness(1);
                txt3.FontSize = 11;
                txt3.Text = Math.Round(face.FaceAttributes.Emotion.Disgust, 2).ToString();
                Grid.SetRow(txt3, index + 1);
                Grid.SetColumn(txt3, 3);
                txt3.Tag = true;

                TextBlock txt4 = new TextBlock();
                txt4.Padding = new Thickness(1);
                txt4.FontSize = 11;
                txt4.Text = Math.Round(face.FaceAttributes.Emotion.Fear, 2).ToString();
                Grid.SetRow(txt4, index + 1);
                Grid.SetColumn(txt4, 4);
                txt4.Tag = true;

                TextBlock txt5 = new TextBlock();
                txt5.Padding = new Thickness(1);
                txt5.FontSize = 11;
                txt5.Text = Math.Round(face.FaceAttributes.Emotion.Happiness, 2).ToString();
                Grid.SetRow(txt5, index + 1);
                Grid.SetColumn(txt5, 5);
                txt5.Tag = true;

                TextBlock txt6 = new TextBlock();
                txt6.Padding = new Thickness(1);
                txt6.FontSize = 11;
                txt6.Text = Math.Round(face.FaceAttributes.Emotion.Neutral, 2).ToString();
                Grid.SetRow(txt6, index + 1);
                Grid.SetColumn(txt6, 6);
                txt6.Tag = true;

                TextBlock txt7 = new TextBlock();
                txt7.Padding = new Thickness(1);
                txt7.FontSize = 11;
                txt7.Text = Math.Round(face.FaceAttributes.Emotion.Sadness, 2).ToString();
                Grid.SetRow(txt7, index + 1);
                Grid.SetColumn(txt7, 7);
                txt7.Tag = true;

                TextBlock txt8 = new TextBlock();
                txt8.Padding = new Thickness(1);
                txt8.FontSize = 11;
                txt8.Text = Math.Round(face.FaceAttributes.Emotion.Surprise, 2).ToString();
                Grid.SetRow(txt8, index + 1);
                Grid.SetColumn(txt8, 8);
                txt8.Tag = true;

                index++;
                gridEmotions.Children.Add(txt0);
                gridEmotions.Children.Add(txt1);
                gridEmotions.Children.Add(txt2);
                gridEmotions.Children.Add(txt3);
                gridEmotions.Children.Add(txt4);
                gridEmotions.Children.Add(txt5);
                gridEmotions.Children.Add(txt6);
                gridEmotions.Children.Add(txt7);
                gridEmotions.Children.Add(txt8);
            }
        }

        /// <summary>
        /// Re-rendering Face Data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cvasMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DisplayEmotionsData(faces, false);
        }

        /// <summary>
        /// Display Emotion Data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void cvasMain_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    if (faces != null)
        //    {
        //        var offset_h = 0.0; var offset_w = 0.0;
        //        var p = 0.0;
        //        var d = cvasMain.ActualHeight / cvasMain.ActualWidth;
        //        var d2 = size_image.Height / size_image.Width;
        //        if (d < d2)
        //        {
        //            offset_h = 0;
        //            offset_w = (cvasMain.ActualWidth - cvasMain.ActualHeight / d2) / 2;
        //            p = cvasMain.ActualHeight / size_image.Height;
        //        }
        //        else
        //        {
        //            offset_w = 0;
        //            offset_h = (cvasMain.ActualHeight - cvasMain.ActualWidth / d2) / 2;
        //            p = cvasMain.ActualWidth / size_image.Width;
        //        }
        //        foreach (var face in faces)
        //        {
        //            Rect rect = new Rect();
        //            rect.Width = face.FaceRectangle.Width * p;
        //            rect.Height = face.FaceRectangle.Height * p;

        //            rect.X = face.FaceRectangle.Left * p + offset_w;
        //            rect.Y = face.FaceRectangle.Top * p + offset_h;

        //            Point point = e.GetPosition(cvasMain);
        //            if (rect.Contains(point))
        //            {
        //                EmotionDataControl edc = new EmotionDataControl();
        //                var dic = new Dictionary<string, double>
        //                {
        //                    {"Anger",face.FaceAttributes.Emotion.Anger },
        //                    {"Contempt",face.FaceAttributes.Emotion.Contempt },
        //                    {"Disgust",face.FaceAttributes.Emotion.Disgust },
        //                    {"Fear",face.FaceAttributes.Emotion.Fear },
        //                    {"Happiness",face.FaceAttributes.Emotion.Happiness },
        //                    {"Neutral",face.FaceAttributes.Emotion.Neutral },
        //                    {"Sadness",face.FaceAttributes.Emotion.Sadness },
        //                    {"Surprise",face.FaceAttributes.Emotion.Surprise },
        //                };
        //                edc.Data = dic;
        //                edc.Width = cvasMain.ActualWidth * 3 / 4;
        //                edc.Height = cvasMain.ActualHeight / 3;

        //                emotionData.Child = edc;
        //                emotionData.VerticalOffset = point.Y;
        //                emotionData.HorizontalOffset = cvasMain.ActualWidth / 8;

        //                emotionData.IsOpen = true;

        //                break;
        //            }
        //        }
        //    }
        //}
    }
}