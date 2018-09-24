using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using FaceApiProxy;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MSEmpotionAPI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FindSimilarCelebrityPage : Page
    {
        //Face API Key
        private const string KeyFace = "34f95dfe9ef7460e9bfbd19987a5b6c3";
        private const string FaceApiroot = "https://westeurope.api.cognitive.microsoft.com/face/v1.0";

        /// <summary>
        /// Temporary stored large face list id.
        /// </summary>
        /// 
        //private const string CelebFacesListId = "f03a14f5-65ff-43b1-be5e-36800680c295";
        //private const string CelebFacesListName = "Celebs";
        //private const string CelebsFolder = "HeleneEtLesGarcons";
        private const string CelebFacesListId = "f03a14f5-65ff-43b1-be5e-36800680c297";
        private const string CelebFacesListName = "TVShows";
        private const string CelebsFolder = "TVShows";
        private const int UploadImagesCount = 1;
        private FindSimilarMatchMode findSimilarMatchMode = FindSimilarMatchMode.matchPerson;
        private readonly ObservableCollection<TrainedFace> _dataSet = new ObservableCollection<TrainedFace>();

        private const string PhotoFileName = "photo.jpg";
        private MediaCapture _mediaCapture;
        private bool _isPreviewing;

        public FindSimilarCelebrityPage()
        {
            this.InitializeComponent();
            _isPreviewing = false;
            initCamera();
        }

        private async void initCamera()
        {
            try
            {
                if (_mediaCapture != null)
                {
                    // Cleanup MediaCapture object
                    if (_isPreviewing)
                    {
                        await _mediaCapture.StopPreviewAsync();
                        captureImage.Source = null;
                        celebImage.Source = null;
                        _isPreviewing = false;
                    }
                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                }

                txtLocation.Text = "Initializing camera to capture audio and video...";
                // Use default initialization
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();

                // Set callbacks for failure and recording limit exceeded
                txtLocation.Text = "Device successfully initialized for video recording!";
                _mediaCapture.Failed += new MediaCaptureFailedEventHandler(mediaCapture_Failed);
                // Start Preview                
                previewElement.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;
                txtLocation.Text = "Camera preview succeeded";

                // Enable buttons for video and photo capture
                btnTakePhoto.IsEnabled = true;

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
                    btnTakePhoto.IsEnabled = false;
                    txtLocation.Text += "\nCheck if camera is diconnected. Try re-launching the app";
                }
            });
        }

        private async void takePhoto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnTakePhoto.IsEnabled = false;
                captureImage.Source = null;
                celebImage.Source = null;
                //store the captured image
                var photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                    PhotoFileName, CreationCollisionOption.GenerateUniqueName);
                ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                await _mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);
                btnTakePhoto.IsEnabled = true;
                txtLocation.Text = "Take Photo succeeded: " + photoFile.Path;
                //display the image
                IRandomAccessStream photoStream = await photoFile.OpenReadAsync();
                BitmapImage bitmap = new BitmapImage();
                bitmap.SetSource(photoStream);
                captureImage.Source = bitmap;
                await SelectFileAsync(photoFile);
            }
            catch (Exception ex)
            {
                txtLocation.Text = ex.Message;
                Cleanup();
            }
        }

        private async void Cleanup()
        {
            if (_mediaCapture != null)
            {
                // Cleanup MediaCapture object
                if (_isPreviewing)
                {
                    await _mediaCapture.StopPreviewAsync();
                    captureImage.Source = null;
                    celebImage.Source = null;
                    _isPreviewing = false;
                }
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
            btnTakePhoto.IsEnabled = false;
        }

        async Task SelectFileAsync(StorageFile photoFile)
        {
            try
            {
                if (photoFile != null)
                {
                    ringLoading.IsActive = true;

                    var stream = await photoFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    var stream_send = stream.CloneStream();
                    var image = new BitmapImage();
                    image.SetSource(stream);

                    //Face service
                    FaceServiceClient faceClient = new FaceServiceClient(KeyFace, FaceApiroot);
                    var response = await FaceApiHelper.FindSimilarFacesAsync(faceClient, stream_send.AsStream(), CelebFacesListId, findSimilarMatchMode);

                    if (response.SimilarFaces.Length > 0)
                    {
                        // Due to legal limitations, Face API does not support images retrieval in any circumstance currently.You need to store the images and maintain the relationship between face ids and images by yourself.

                        var matches =
                            from c in response.SimilarFaces
                            join p in _dataSet on c.PersistedFaceId equals p.Face.FaceId into ps
                            from p in ps.DefaultIfEmpty()
                            select new { Confidence = c.Confidence, PersonName = p == null ? "(No matching face)" : p.PersonName, FilePath = p.FilePath, FaceId = c.PersistedFaceId };

                        var match = matches.OrderByDescending(m => m.Confidence).FirstOrDefault();
                        if (match != null)
                        {
                            txtLocation.Text = $"Face {{{match.PersonName}}} was recognized with confidence: {match.Confidence}. PersistedFaceId: {match.FaceId}";
                            var matchFile = await StorageFile.GetFileFromPathAsync(match.FilePath);
                            IRandomAccessStream photoStream = await matchFile.OpenReadAsync();
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.SetSource(photoStream);
                            celebImage.Source = bitmap;
                        }
                        else
                        {
                            txtLocation.Text = "Response: No matching person.";
                        }
                    }
                    else
                    {
                        txtLocation.Text = "Response: No matching person.";
                    }
                }
            }
            catch (Exception ex)
            {
                txtLocation.Text = ex.Message;
            }
            finally
            {
                ringLoading.IsActive = false;
            }
        }

        private async void UploadFacesAsync()
        {
            btnUpload.IsEnabled = false;
            btnLoad.IsEnabled = false;
            btnTakePhoto.IsEnabled = false;
            ringLoading.IsActive = true;

            //Face service
            FaceServiceClient faceClient = new FaceServiceClient(KeyFace, FaceApiroot);

            try
            {
                txtLocation.Text = $"Request: Uploading Large Faces List \"{CelebFacesListId}\"";
                var trainedFaces = await FaceDatasetHelper.UploadFacesAsync(faceClient, CelebsFolder, CelebFacesListId, CelebFacesListName, imagesCount: UploadImagesCount);

                // Write dataset
                await FaceDatasetHelper.WriteDatasetAsync(trainedFaces, CelebFacesListName);

                // Start train large face list.
                txtLocation.Text = $"Request: Training Large Face List \"{CelebFacesListId}\"";
                await faceClient.TrainLargeFaceListAsync(CelebFacesListId);

                // Wait until train completed
                while (true)
                {
                    var status = await faceClient.GetLargeFaceListTrainingStatusAsync(CelebFacesListId);
                    txtLocation.Text =
                        $"Response: Success. Large Face List \"{CelebFacesListId}\" training process is {status.Status}";
                    if (status.Status == Status.Running)
                    {
                        await Task.Delay(1000);
                        continue;
                    }
                    if (status.Status == Status.Failed)
                    {
                        txtLocation.Text =
                            $"Response: Failed. Large Face List \"{CelebFacesListId}\" training process is {status.Status}";
                    }
                    break;
                }
            }
            catch (FaceAPIException ex)
            {
                txtLocation.Text = $"Response: {ex.ErrorCode}. {ex.ErrorMessage}";
            }
            catch (Exception ex)
            {
                txtLocation.Text = ex.Message;
                throw;
            }
            finally
            {

                btnUpload.IsEnabled = true;
                btnLoad.IsEnabled = true;
                btnTakePhoto.IsEnabled = true;
                ringLoading.IsActive = false;
            }
        }

        private async void LoadDataSetFromFileAsync()
        {
            btnUpload.IsEnabled = false;
            btnLoad.IsEnabled = false;
            btnTakePhoto.IsEnabled = false;
            ringLoading.IsActive = true;

            txtLocation.Text = $"Request: Loading DataSet for List \"{CelebFacesListId}\"";

            await FaceDatasetHelper.LoadDatasetAsync(this._dataSet, CelebFacesListName);

            btnUpload.IsEnabled = true;
            btnLoad.IsEnabled = true;
            btnTakePhoto.IsEnabled = true;
            ringLoading.IsActive = false;

            txtLocation.Text = $"Response: Succes. DataSet loaded for List \"{CelebFacesListId}\"";
        }

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            UploadFacesAsync();
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadDataSetFromFileAsync();
        }
    }
}