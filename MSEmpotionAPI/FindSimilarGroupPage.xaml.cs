using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI.Core;
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
    public sealed partial class FindSimilarGroupPage : Page
    {
        private static readonly string[] DefaultMediaFileExtensions = {
            // picture
            ".jpg",
            ".png",
            ".bmp",
        };

        //Face API Key
        string key_face = "34f95dfe9ef7460e9bfbd19987a5b6c3";
        string face_apiroot = "https://westeurope.api.cognitive.microsoft.com/face/v1.0";

        private Face[] faces;

        StorageFolder currentFolder;

        private MediaCapture mediaCapture;
        private StorageFile photoFile;
        private const string PHOTO_FILE_NAME = "photo.jpg";
        private bool isPreviewing;

        /// <summary>
        /// Temporary stored large face list id.
        /// </summary>
        /// 
        private string _celebFacesListId = "f03a14f5-65ff-43b1-be5e-36800680c303";
        private string _celebFacesListName = "Series";
        private const string CelebsFolder = "Series";

        private int _sumUploadedFaces;

        public FindSimilarGroupPage()
        {
            this.InitializeComponent();
            isPreviewing = false;
            initCamera();
        }

        private async void initCamera()
        {
            try
            {
                if (mediaCapture != null)
                {
                    // Cleanup MediaCapture object
                    if (isPreviewing)
                    {
                        await mediaCapture.StopPreviewAsync();
                        captureImage.Source = null;
                        isPreviewing = false;
                    }
                    mediaCapture.Dispose();
                    mediaCapture = null;
                }

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
                isPreviewing = true;
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
                //store the captured image
                photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                    PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);
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
            if (mediaCapture != null)
            {
                // Cleanup MediaCapture object
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                    captureImage.Source = null;
                    isPreviewing = false;
                }
                mediaCapture.Dispose();
                mediaCapture = null;
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
                    FaceServiceClient faceClient = new FaceServiceClient(key_face, face_apiroot);
                    var response = await FaceApiHelper.IdentifyPersonAsync(faceClient, stream_send.AsStream(), this._celebFacesListId);

                    if (response.Candidates.Length > 0)
                    {
                        // Due to legal limitations, Face API does not support images retrieval in any circumstance currently.You need to store the images and maintain the relationship between face ids and images by yourself.

                        var personsFolder = await KnownFolders.PicturesLibrary.GetFolderAsync(CelebsFolder);
                        var _dataSet = await faceClient.ListPersonsAsync(this._celebFacesListId);

                        var matches =
                            from c in response.Candidates
                            join p in _dataSet on c.PersonId equals p.PersonId into ps
                            from p in ps.DefaultIfEmpty()
                            select new
                            {
                                Confidence = c.Confidence,
                                PersonName = p == null ? "(No matching face)" : p.Name,
                                FaceId = c.PersonId
                            };

                        var match = matches.OrderByDescending(m => m.Confidence).FirstOrDefault();
                        if (match != null)
                        {
                            txtLocation.Text = $"Person {{{match.PersonName}}} was recognized with confidence: {match.Confidence}. PersistedFaceId: {match.FaceId}";
                            var personFolder = await personsFolder.GetFolderAsync(match.PersonName);
                            // Take first image in folder

                            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, DefaultMediaFileExtensions);
                            queryOptions.FolderDepth = FolderDepth.Shallow;

                            var query = personFolder.CreateFileQueryWithOptions(queryOptions);
                            var imgFiles = await query.GetFilesAsync();
                            var matchFile = imgFiles.First();
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
            btnTakePhoto.IsEnabled = false;
            ringLoading.IsActive = true;

            _sumUploadedFaces = 0;

            //Face service
            FaceServiceClient faceClient = new FaceServiceClient(key_face, face_apiroot);

            try
            {
                txtLocation.Text = $"Request: Uploading Groups \"{this._celebFacesListId}\"";

                try
                {
                    await faceClient.CreatePersonGroupAsync(this._celebFacesListId, this._celebFacesListName);
                }
                catch (FaceAPIException ex)
                {
                    if (ex.ErrorCode != "PersonGroupExists")
                    {
                        throw;
                    }
                }

                var largeFacesFolder = await KnownFolders.PicturesLibrary.GetFolderAsync(CelebsFolder);
                var folders = await largeFacesFolder.GetFoldersAsync();

                foreach (var folder in folders)
                {
                    await UploadPersonFacesFromFolderAsync(faceClient, folder);
                }

                // Start train large face list.
                txtLocation.Text = $"Request: Training Group \"{this._celebFacesListId}\"";
                await faceClient.TrainPersonGroupAsync(this._celebFacesListId);

                // Wait until train completed
                while (true)
                {
                    var status = await faceClient.GetPersonGroupTrainingStatusAsync(this._celebFacesListId);
                    txtLocation.Text =
                        $"Response: Success. Group \"{this._celebFacesListId}\" training process is {status.Status}";
                    if (status.Status == Status.Running)
                    {
                        await Task.Delay(1000);
                        continue;
                    }
                    if (status.Status == Status.Failed)
                    {
                        txtLocation.Text =
                            $"Response: Failed. Group \"{this._celebFacesListId}\" training process is {status.Status}";
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
                throw;
            }
            finally
            {
                btnUpload.IsEnabled = true;
                btnTakePhoto.IsEnabled = true;
                ringLoading.IsActive = false;
            }
        }

        private async Task UploadPersonFacesFromFolderAsync(FaceServiceClient faceClient, StorageFolder folder)
        {
            Guid personId;
            bool rateLimitExceeded;
            do
            {
                rateLimitExceeded = false;
                try
                {
                    // Get or create person
                    var persons = await faceClient.ListPersonsInPersonGroupAsync(this._celebFacesListId, start: "0");
                    var person = persons.FirstOrDefault(p => p.Name == folder.Name);
                    if (person != null)
                    {
                        personId = person.PersonId;
                    }
                    else
                    {
                        personId = await FaceApiHelper.CreatePersonAsync(faceClient, folder.Name, this._celebFacesListId);
                    }
                }
                catch (Microsoft.ProjectOxford.Face.FaceAPIException e)
                {
                    if (e.ErrorCode == "RateLimitExceeded" || e.ErrorMessage.Contains("There is a conflict operation on resource"))
                    {
                        rateLimitExceeded = true;
                        await Task.Delay(1000);
                    }
                    else
                    {
                        throw;
                    }
                }
            } while (rateLimitExceeded == true);


            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, DefaultMediaFileExtensions);
            queryOptions.FolderDepth = FolderDepth.Shallow;

            var query = folder.CreateFileQueryWithOptions(queryOptions);
            var imgFiles = await query.GetFilesAsync();
            await UploadPersonFacesAsync(faceClient, personId, imgFiles);
        }

        private async Task UploadPersonFacesAsync(FaceServiceClient faceClient, Guid personId, IEnumerable<string> faceLinks)
        {
            int i = 0;
            int nbUploadedFaces = 0;
            foreach (var faceLink in faceLinks)
            {
                var success = await FaceDatasetHelper.UploadPersonFaceAsync(faceClient, this._celebFacesListId, personId, faceLink);
                if (success)
                {
                    _sumUploadedFaces++;
                    await Window.Current.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            txtLocation.Text =
                                $"Uploaded faces count: {_sumUploadedFaces}";
                        });
                    nbUploadedFaces++;
                    if (nbUploadedFaces >= 2)
                    {
                        break;
                    }
                }
            }
        }

        private async Task UploadPersonFacesAsync(FaceServiceClient faceClient, Guid personId, IEnumerable<StorageFile> faceFiles)
        {
            int i = 0;
            int nbUploadedFaces = 0;
            foreach (var faceImage in faceFiles)
            {
                var success = await FaceDatasetHelper.UploadPersonFaceAsync(faceClient, this._celebFacesListId, personId, faceImage);
                if (success)
                {
                    _sumUploadedFaces++;
                    await Window.Current.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            txtLocation.Text =
                                $"Uploaded faces count: {_sumUploadedFaces}";
                        });
                    nbUploadedFaces++;
                    if (nbUploadedFaces >= 2)
                    {
                        break;
                    }
                }
            }
        }

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            UploadFacesAsync();
        }
    }
}