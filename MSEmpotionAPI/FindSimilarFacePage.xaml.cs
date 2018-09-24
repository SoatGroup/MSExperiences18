using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using FaceApiProxy;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MSEmpotionAPI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FindSimilarFacePage : Page
    {
        //Face API Key
        string key_face = "34f95dfe9ef7460e9bfbd19987a5b6c3";
        string face_apiroot = "https://westeurope.api.cognitive.microsoft.com/face/v1.0";

        Size size_image;
        Face[] faces;

        StorageFolder currentFolder;
        StorageFile Picker_SelectedFile;
        private QueryOptions queryOptions;

        private MediaCapture mediaCapture;
        private StorageFile photoFile;
        private readonly string PHOTO_FILE_NAME = "photo.jpg";
        private bool isPreviewing;

        /// <summary>
        /// Temporary stored large face list id.
        /// </summary>
        private string _largeFaceListId = Guid.NewGuid().ToString();
        /// <summary>
        /// Faces collection which will be used to find similar from
        /// </summary>
        private ObservableCollection<TrainedFace> _facesCollection = new ObservableCollection<TrainedFace>();

        public FindSimilarFacePage()
        {
            this.InitializeComponent();
            InitReferenceFaces();
            queryOptions = new QueryOptions(CommonFileQuery.OrderByName, mediaFileExtensions);
            queryOptions.FolderDepth = FolderDepth.Shallow;
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
                Picker_SelectedFile = photoFile;
                SelectFile();
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

        private string[] mediaFileExtensions = {
            // picture
            ".jpg",
            ".png",
            ".bmp",
        };

        //Browse Button Click Event
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            Picker_Show();
        }

        private async void Picker_Show()
        {
            await Picker_Populate();
            grdPicker.Visibility = Visibility.Visible;
        }

        private async Task Picker_Populate()
        {
            Picker_SelectedFile = null;
            if (currentFolder == null)
            {
                lstFiles.Items.Clear();
                lstFiles.Items.Add(">Documents");
                lstFiles.Items.Add(">Pictures");
                lstFiles.Items.Add(">Music");
                lstFiles.Items.Add(">Videos");
                lstFiles.Items.Add(">RemovableStorage");
            }
            else
            {
                lstFiles.Items.Clear();
                lstFiles.Items.Add(">..");
                var folders = await currentFolder.GetFoldersAsync();
                foreach (var f in folders)
                {
                    lstFiles.Items.Add(">" + f.Name);
                }
                var query = currentFolder.CreateFileQueryWithOptions(queryOptions);
                var files = await query.GetFilesAsync();
                foreach (var f in files)
                {
                    lstFiles.Items.Add(f.Name);
                }
            }
        }

        //Clear Button Click Event
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            txtFileName.Text = "";
        }

        //Select Button Click Event
        private async void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (lstFiles.SelectedItem != null)
            {
                if (await Picker_BrowseTo(lstFiles.SelectedItem.ToString()))
                {
                    SelectFile();
                }
                else
                {
                    lstFiles.Focus(FocusState.Keyboard);
                }
            }
        }

        async void SelectFile()
        {
            Picker_Hide();
            try
            {
                if (Picker_SelectedFile != null)
                {
                    txtFileName.Text = Picker_SelectedFile.Path;
                    var stream = await Picker_SelectedFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    var stream_send = stream.CloneStream();
                    var image = new BitmapImage();
                    image.SetSource(stream);
                    imgPhoto.Source = image;
                    size_image = new Size(image.PixelWidth, image.PixelHeight);

                    ringLoading.IsActive = true;

                    //Face service
                    FaceServiceClient f_client = new FaceServiceClient(key_face, face_apiroot);

                    faces = await f_client.DetectAsync(stream_send.AsStream());
                    if (faces != null)
                    {
                        // Select biggest face
                        var face = faces.OrderByDescending(f => f.FaceRectangle.Height * f.FaceRectangle.Width).First();
                        var faceId = face.FaceId;

                        txtLocation.Text = $"Request: Finding similar faces in Personal Match Mode for face {faceId}";

                        try
                        {
                            // Call find facial match similar REST API, the result faces the top N with the highest similar confidence 
                            const int requestCandidatesCount = 4;
                            var result = await f_client.FindSimilarAsync(faceId, largeFaceListId: this._largeFaceListId, mode: FindSimilarMatchMode.matchPerson, maxNumOfCandidatesReturned: requestCandidatesCount);

                            if (result.Length > 0)
                            {
                                var matchingFace = result.OrderByDescending(m => m.Confidence).FirstOrDefault();
                                var trainedFace =
                                    _facesCollection.First(f => f.Face.FaceId == matchingFace.PersistedFaceId);
                                txtLocation.Text =
                                    $"Face {{{matchingFace.PersistedFaceId}}} was recognized with confidence: {matchingFace.Confidence}. File: {trainedFace.FilePath}, Person: {trainedFace.PersonName}";
                            }
                            else
                            {

                                txtLocation.Text = "Response: No matching person.";
                            }

                            //// Update "matchFace" similar results collection for rendering
                            //var faceSimilarResults = new FindSimilarResult();
                            //faceSimilarResults.Faces = new ObservableCollection<Face>();
                            //faceSimilarResults.QueryFace = new Face()
                            //{
                            //    ImageFile = SelectedFile,
                            //    Top = f.FaceRectangle.Top,
                            //    Left = f.FaceRectangle.Left,
                            //    Width = f.FaceRectangle.Width,
                            //    Height = f.FaceRectangle.Height,
                            //    FaceId = faceId.ToString(),
                            //};
                            //foreach (var fr in result)
                            //{
                            //    var candidateFace = FacesCollection.First(ff => ff.FaceId == fr.PersistedFaceId.ToString());
                            //    Face newFace = new Face();
                            //    newFace.ImageFile = candidateFace.ImageFile;
                            //    newFace.Confidence = fr.Confidence;
                            //    newFace.FaceId = candidateFace.FaceId;
                            //    faceSimilarResults.Faces.Add(newFace);
                            //}

                            //MainWindow.Log("Response: Found {0} similar faces for face {1}", faceSimilarResults.Faces.Count, faceId);

                            //FindSimilarMatchFaceCollection.Add(faceSimilarResults);
                        }
                        catch (FaceAPIException ex)
                        {
                            txtLocation.Text = $"Response: {ex.ErrorCode}. {ex.ErrorMessage}";
                        }
                    }

                    //hide preview
                    if (stpPreview.Visibility == Visibility.Collapsed)
                    {
                        stpPreview.Visibility = Visibility.Visible;
                        btnShow.Content = "Hide Preview";
                    }
                    else
                    {
                        stpPreview.Visibility = Visibility.Collapsed;
                        btnShow.Content = "Show Preview";
                    }

                    ringLoading.IsActive = false;
                }
            }
            catch (Exception ex)
            {
                //lblError.Text = ex.Message;
                //lblError.Visibility = Visibility.Visible;
            }
        }

        private async void InitReferenceFaces()
        {
            btnTakePhoto.IsEnabled = false;
            btnSelect.IsEnabled = false;
            ringLoading.IsActive = true;

            //Face service
            FaceServiceClient f_client = new FaceServiceClient(key_face, face_apiroot);

            try
            {
                await f_client.CreateLargeFaceListAsync(this._largeFaceListId, this._largeFaceListId, "large face list for sample");

                var largeFacesFolder = await KnownFolders.PicturesLibrary.GetFolderAsync("LargeFaces");
                var folders = await largeFacesFolder.GetFoldersAsync();
                foreach (var folder in folders)
                {
                    var files = await folder.GetFilesAsync();
                    foreach (var file in files)
                    {
                        // Upload load
                        using (var stream = await file.OpenReadAsync())
                        {
                            var detectedFaces = await f_client.DetectAsync(stream.CloneStream().AsStream());
                            if (detectedFaces.Length > 0)
                            {
                                // Select the biggest face in the photo
                                var detectedFace = detectedFaces.OrderByDescending(f => f.FaceRectangle.Width * f.FaceRectangle.Height).First();
                                var uploadedFace = await f_client.AddFaceToLargeFaceListAsync(this._largeFaceListId, stream.AsStream());

                                _facesCollection.Add(new TrainedFace(
                                    new Face
                                    {
                                        FaceId = uploadedFace.PersistedFaceId,
                                        FaceRectangle = detectedFace.FaceRectangle,
                                        FaceAttributes = new FaceAttributes()
                                    },
                                    folder.Name,
                                    file.Name));
                            }
                        }
                    }
                }

                // Start train large face list.
                txtLocation.Text = $"Request: Training Large Face List \"{this._largeFaceListId}\"";
                await f_client.TrainLargeFaceListAsync(this._largeFaceListId);

                // Wait until train completed
                while (true)
                {
                    await Task.Delay(1000);
                    var status = await f_client.GetLargeFaceListTrainingStatusAsync(this._largeFaceListId);
                    txtLocation.Text = $"Response: Success. Large Face List \"{this._largeFaceListId}\" training process is {status.Status}";
                    if (status.Status != Status.Running)
                    {
                        break;
                    }
                }
            }
            catch (FaceAPIException ex)
            {
                txtLocation.Text = $"Response: {ex.ErrorCode}. {ex.ErrorMessage}";
            }

            btnTakePhoto.IsEnabled = true;
            btnSelect.IsEnabled = true;
            ringLoading.IsActive = false;
        }

        //Open Button Click Event
        private async void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //lblError.Visibility = Visibility.Collapsed;
                var file = await StorageFile.GetFileFromPathAsync(txtFileName.Text);
                var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            }
            catch (Exception ex)
            {
                //lblError.Text = ex.Message;
                //lblError.Visibility = Visibility.Visible;
            }
        }

        private void txtFileName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //lblError.Visibility = Visibility.Collapsed;
        }

        private void Picker_Hide()
        {
            SetMainPageControlEnableState(true);
            grdPicker.Visibility = Visibility.Collapsed;
        }

        private void SetMainPageControlEnableState(bool isEnabled)
        {
            btnBrowse.IsEnabled = isEnabled;
            btnClear.IsEnabled = isEnabled;
            btnOpen.IsEnabled = isEnabled;
            txtFileName.IsEnabled = isEnabled;
        }

        private async Task<bool> Picker_BrowseTo(string filename)
        {
            Picker_SelectedFile = null;
            if (currentFolder == null)
            {
                switch (filename)
                {
                    case ">Documents":
                        currentFolder = KnownFolders.DocumentsLibrary;
                        break;
                    case ">Pictures":
                        currentFolder = KnownFolders.PicturesLibrary;
                        break;
                    case ">Music":
                        currentFolder = KnownFolders.MusicLibrary;
                        break;
                    case ">Videos":
                        currentFolder = KnownFolders.VideosLibrary;
                        break;
                    case ">RemovableStorage":
                        currentFolder = KnownFolders.RemovableDevices;
                        break;
                    default:
                        throw new Exception("unexpected");
                }
                lblBreadcrumb.Text = "> " + filename.Substring(1);
            }
            else
            {
                if (filename == ">..")
                {
                    await Picker_FolderUp();
                }
                else if (filename[0] == '>')
                {
                    var foldername = filename.Substring(1);
                    var folder = await currentFolder.GetFolderAsync(foldername);
                    currentFolder = folder;
                    lblBreadcrumb.Text += " > " + foldername;
                }
                else
                {
                    Picker_SelectedFile = await currentFolder.GetFileAsync(filename);
                    return true;
                }
            }
            await Picker_Populate();
            return false;
        }

        async Task Picker_FolderUp()
        {
            if (currentFolder == null)
            {
                return;
            }
            try
            {
                var folder = await currentFolder.GetParentAsync();
                currentFolder = folder;
                if (currentFolder == null)
                {
                    lblBreadcrumb.Text = ">";
                }
                else
                {
                    var breadcrumb = lblBreadcrumb.Text;
                    breadcrumb = breadcrumb.Substring(0, breadcrumb.LastIndexOf('>') - 1);
                    lblBreadcrumb.Text = breadcrumb;
                }
            }
            catch (Exception)
            {
                currentFolder = null;
                lblBreadcrumb.Text = ">";
            }
        }

        //Double Tapped Event for listview
        private async void lstFiles_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (lstFiles.SelectedItem != null)
            {
                if (await Picker_BrowseTo(lstFiles.SelectedItem.ToString()))
                {
                    SelectFile();
                }
                else
                {
                    lstFiles.Focus(FocusState.Keyboard);
                }
            }
        }

        //Keyup enevt for listview
        private async void lstFiles_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (lstFiles.SelectedItem != null && e.Key == Windows.System.VirtualKey.Enter)
            {
                if (await Picker_BrowseTo(lstFiles.SelectedItem.ToString()))
                {
                    SelectFile();
                }
                else
                {
                    lstFiles.Focus(FocusState.Keyboard);
                }
            }
        }

        //Cancel Button Click Event
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Picker_Hide();
        }

        private async void imgPhoto_ImageOpened(object sender, RoutedEventArgs e)
        {
            size_image = new Size((imgPhoto.Source as BitmapImage).PixelWidth, (imgPhoto.Source as BitmapImage).PixelHeight);

            FaceServiceClient f_client = new FaceServiceClient(key_face);

            var requiedFaceAttributes = new FaceAttributeType[] {
                                FaceAttributeType.Age,
                                FaceAttributeType.Gender,
                                FaceAttributeType.Smile,
                                FaceAttributeType.FacialHair,
                                FaceAttributeType.HeadPose,
                                FaceAttributeType.Emotion,
                                FaceAttributeType.Glasses
                                };
            var faces_task = f_client.DetectAsync(txtLocation.Text, true, true, requiedFaceAttributes);

            faces = await faces_task;

            ringLoading.IsActive = false;
        }

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            if (stpPreview.Visibility == Visibility.Collapsed)
            {
                stpPreview.Visibility = Visibility.Visible;
                btnShow.Content = "Hide Preview";
            }
            else
            {
                stpPreview.Visibility = Visibility.Collapsed;
                btnShow.Content = "Show Preview";
            }
        }
    }
}