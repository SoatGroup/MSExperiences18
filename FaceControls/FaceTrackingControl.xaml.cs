using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.Toolkit.Uwp.Connectivity;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace FaceControls
{
    public sealed partial class FaceTrackingControl : UserControl
    {
        private readonly string _smileyAssetPath = "ms-appx:///Assets/smiley.png";
        private readonly SolidColorBrush _smileyColor = Application.Current.Resources["FaceSmileyColor"] as SolidColorBrush;
        private readonly SolidColorBrush _faceBoundingBoxColor = Application.Current.Resources["FaceBoudingBoxColor"] as SolidColorBrush;

        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;
        private FaceDetectionEffect _faceDetectionEffect;
        private VideoEncodingProperties _previewProperties;
        private FaceTracker _faceTracker;
        private bool _isPreviewing;
        private readonly bool _mirroringPreview = true;
        private readonly BitmapIcon _smiley;
        private readonly BitmapIcon _smileyNeutral;

        public FaceServiceClient FaceClient;

        //0 for false, 1 for true.
        private static int _isCheckingSmile;
        private DateTime? _lastSmileCheck;

        public string Status { get; set; }
        public bool IsCheckSmileEnabled { get; set; }
        public MediaCapture VideoCapture { get; private set; }
        /// <summary>
        /// Occurs when a face is detected. See FaceDetectedEventArgs
        /// </summary>
        public event TypedEventHandler<FaceDetectionEffect, FaceDetectedEventArgs> FaceDetected;
        public event EventHandler<Face> SmileDetected;

        public FaceTrackingControl()
        {
            InitializeComponent();

            _isPreviewing = false;

            _smiley = new BitmapIcon
            {
                UriSource = new Uri(_smileyAssetPath),
                Foreground = _smileyColor
            };

            _smileyNeutral = new BitmapIcon {Foreground = _smileyColor};
        }

        public async Task InitCameraAsync(string cameraName, double screenRatio)
        {
            try
            {
                Cleanup();

                Status = "Initializing camera to capture audio and video...";

                VideoCapture = new MediaCapture();
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings()
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video
                };

                var allCameras = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                var selectedCamera = allCameras.FirstOrDefault(c => c.Name == cameraName);
                if (selectedCamera != null)
                {
                    settings.VideoDeviceId = selectedCamera.Id;
                }
                
                await VideoCapture.InitializeAsync(settings);

                // Set callbacks for failure and recording limit exceeded
                Status = "Device successfully initialized for video recording!";
                VideoCapture.Failed += mediaCapture_Failed;

                // Start Preview                
                PreviewControl.Source = VideoCapture;
                PreviewControl.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                // Also mirror the canvas if the preview is being mirrored
                FacesCanvas.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;


                // Query all properties of the specified stream type 
                var props = VideoCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview);
                IEnumerable<StreamResolution> allStreamProperties = props.Select(x => new StreamResolution(x));
                // Order them by resolution then frame rate
                allStreamProperties = allStreamProperties.Where(p => p.AspectRatio == screenRatio).OrderByDescending(x => x.FrameRate).ThenByDescending(x => x.Height * x.Width);
                await VideoCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, allStreamProperties.First().EncodingProperties);


                await VideoCapture.StartPreviewAsync();
                _previewProperties = VideoCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

                // Initialize the preview to the current orientation
                if (_previewProperties != null)
                {
                    _displayOrientation = _displayInformation.CurrentOrientation;
                }
                _isPreviewing = true;
                Status = "Camera preview succeeded";

                await InitFaceTrackerAsync();

                _faceTracker = await FaceTracker.CreateAsync();
            }
            catch (Exception ex)
            {
                Status = "Unable to initialize camera for audio/video mode: " + ex.Message;
            }
        }

        public void StartFaceTracking()
        {
            // Start detecting faces
            _faceDetectionEffect.Enabled = true;
        }

        public void StopFaceTracking()
        {
            _faceDetectionEffect.Enabled = false;

            CleanCanvas();
        }

        public void CleanCanvas()
        {
            // Remove any existing rectangles from previous events
            FacesCanvas.Children.Clear();
        }

        /// <summary>
        /// Start Face detection
        /// </summary>
        /// <returns></returns>
        private async Task InitFaceTrackerAsync()
        {
            // Create the definition, which will contain some initialization settings
            var definition = new FaceDetectionEffectDefinition();
            
            // To ensure preview smoothness, do not delay incoming samples
            definition.SynchronousDetectionEnabled = false;
            
            // In this scenario, choose detection speed over accuracy
            definition.DetectionMode = FaceDetectionMode.HighPerformance;
            
            // Add the effect to the preview stream
            _faceDetectionEffect = (FaceDetectionEffect)await VideoCapture.AddVideoEffectAsync(definition, MediaStreamType.VideoPreview);

            // Choose the shortest interval between detection events
            _faceDetectionEffect.DesiredDetectionInterval = TimeSpan.FromMilliseconds(33);

            // Register for face detection events
            _faceDetectionEffect.FaceDetected += FaceDetectionEffect_FaceDetected;

            Status = "Face tracker sucessfully initialized!";
        }

        private async void FaceDetectionEffect_FaceDetected(FaceDetectionEffect sender, FaceDetectedEventArgs args)
        {
            if (args.ResultFrame.DetectedFaces.Any())
            {
                var biggestFace = args.ResultFrame.DetectedFaces.OrderByDescending(f => f.FaceBox.Height * f.FaceBox.Width).FirstOrDefault();
                if (biggestFace != null)
                {
                    var faceBounds = new BitmapBounds
                    {
                        X = biggestFace.FaceBox.X,
                        Y = biggestFace.FaceBox.Y,
                        Height = biggestFace.FaceBox.Height,
                        Width = biggestFace.FaceBox.Width
                    };
                    // Check if face is not too big for face bounding box extrapolation
                    if (false == TryExtendFaceBounds(
                            (int)_previewProperties.Width, (int)_previewProperties.Height,
                            Constants.FaceBoxRatio, ref faceBounds))
                    {
                        return;
                    }
                }

                // Ask the UI thread to render the face bounding boxes
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => HighlightDetectedFaces(args.ResultFrame.DetectedFaces));
                FaceDetected?.Invoke(sender, args);

                if (IsCheckSmileEnabled)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await CheckSmileAsync());
                }
            }
        }

        private async void mediaCapture_Failed(MediaCapture currentCaptureObject, MediaCaptureFailedEventArgs currentFailure)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    Status = "MediaCaptureFailed: " + currentFailure.Message;
                }
                catch (Exception)
                {
                    // ignored
                }
                finally
                {
                    Status += "\nCheck if camera is diconnected. Try re-launching the app";
                }
            });
        }

        private async void Cleanup()
        {
            if (VideoCapture != null)
            {
                // Cleanup MediaCapture object
                if (_isPreviewing)
                {
                    await VideoCapture.StopPreviewAsync();
                    PreviewControl.Source = null;
                    _isPreviewing = false;
                }
                _faceDetectionEffect = null;
                VideoCapture.Dispose();
                VideoCapture = null;
            }
        }

        #region Face detection helpers

        /// <summary>
        /// Iterates over all detected faces, creating and adding Rectangles to the FacesCanvas as face bounding boxes
        /// </summary>
        /// <param name="faces">The list of detected faces from the FaceDetected event of the effect</param>
        private void HighlightDetectedFaces(IReadOnlyList<DetectedFace> faces)
        {
            CleanCanvas();

            if (faces.Count < 1)
                return;

            var orderedFaces = faces.OrderByDescending(f => f.FaceBox.Height * f.FaceBox.Width).ToList();
            // For each detected face
            for (int i = 0; i < orderedFaces.Count; i++)
            {
                // Face coordinate units are preview resolution pixels, which can be a different scale from our display resolution, so a conversion may be necessary
                Rectangle faceBoundingBox = ConvertPreviewToUiRectangle(faces[i].FaceBox);

                if (i != 0)
                {
                    // Set bounding box stroke properties
                    faceBoundingBox.StrokeThickness = 2;
                    // Highlight the first face in the set
                    faceBoundingBox.Stroke = _faceBoundingBoxColor;
                    // Add grid to canvas containing all face UI objects
                    FacesCanvas.Children.Add(faceBoundingBox);
                }
                else
                {
                    var left = Canvas.GetLeft(faceBoundingBox);
                    var top = Canvas.GetTop(faceBoundingBox);
                    var faceIcon = IsCheckSmileEnabled ? _smiley : _smileyNeutral;
                    Canvas.SetLeft(faceIcon, left - faceBoundingBox.Width / 4);
                    Canvas.SetTop(faceIcon, top - faceBoundingBox.Height / 4);
                    faceIcon.Width = faceBoundingBox.Width * 1.5;
                    faceIcon.Height = faceBoundingBox.Height * 1.5;
                    FacesCanvas.Children.Add(faceIcon);
                }
            }
        }

        private async Task CheckSmileAsync()
        {
            if (IsCheckSmileEnabled && _isCheckingSmile != 1 && (_lastSmileCheck == null || _lastSmileCheck < DateTime.Now.AddSeconds(-1)))
            {
                // 0 indicates that the method is not in use.
                if (0 == Interlocked.Exchange(ref _isCheckingSmile, 1))
                {
                    _lastSmileCheck = DateTime.Now;

                    var requiedFaceAttributes = new[] { FaceAttributeType.Smile };
                    if (VideoCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) is VideoEncodingProperties previewProperties)
                    {
                        double scale = 480d / previewProperties.Height;
                        VideoFrame videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)(previewProperties.Width * scale), 480);
                        using (var frame = await VideoCapture.GetPreviewFrameAsync(videoFrame))
                        {
                            if (frame.SoftwareBitmap != null)
                            {
                                var bitmap = frame.SoftwareBitmap;

                                InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
                                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                                encoder.SetSoftwareBitmap(bitmap);

                                await encoder.FlushAsync();

                                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                                {
                                    var detect = await FaceClient.DetectAsync(stream.AsStream(), false, false, requiedFaceAttributes);
                                    if (detect.Any())
                                    {
                                        var biggestFace = detect.OrderByDescending(f => f.FaceRectangle.Height * f.FaceRectangle.Width).First();
                                        if (biggestFace.FaceAttributes.Smile > 0.5)//TODO add || no internet
                                        {
                                            SmileDetected?.Invoke(this, biggestFace);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    Interlocked.Exchange(ref _isCheckingSmile, 0);
                }
            }
        }

        /// <summary>
        /// Takes face information defined in preview coordinates and returns one in UI coordinates, taking
        /// into account the position and size of the preview control.
        /// </summary>
        /// <param name="faceBoxInPreviewCoordinates">Face coordinates as retried from the FaceBox property of a DetectedFace, in preview coordinates.</param>
        /// <returns>Rectangle in UI (CaptureElement) coordinates, to be used in a Canvas control.</returns>
        private Rectangle ConvertPreviewToUiRectangle(BitmapBounds faceBoxInPreviewCoordinates)
        {
            var result = new Rectangle();
            var previewStream = _previewProperties;

            // If there is no available information about the preview, return an empty rectangle, as re-scaling to the screen coordinates will be impossible
            if (previewStream == null) return result;

            // Similarly, if any of the dimensions is zero (which would only happen in an error case) return an empty rectangle
            if (previewStream.Width == 0 || previewStream.Height == 0) return result;

            double streamWidth = previewStream.Width;
            double streamHeight = previewStream.Height;

            // For portrait orientations, the width and height need to be swapped
            if (_displayOrientation == DisplayOrientations.Portrait || _displayOrientation == DisplayOrientations.PortraitFlipped)
            {
                streamHeight = previewStream.Width;
                streamWidth = previewStream.Height;
            }

            // Get the rectangle that is occupied by the actual video feed
            var previewInUi = GetPreviewStreamRectInControl(previewStream, PreviewControl);

            // Scale the width and height from preview stream coordinates to window coordinates
            result.Width = (faceBoxInPreviewCoordinates.Width / streamWidth) * previewInUi.Width;
            result.Height = (faceBoxInPreviewCoordinates.Height / streamHeight) * previewInUi.Height;

            // Scale the X and Y coordinates from preview stream coordinates to window coordinates
            var x = (faceBoxInPreviewCoordinates.X / streamWidth) * previewInUi.Width;
            var y = (faceBoxInPreviewCoordinates.Y / streamHeight) * previewInUi.Height;
            Canvas.SetLeft(result, x);
            Canvas.SetTop(result, y);

            return result;
        }

        /// <summary>
        /// Calculates the size and location of the rectangle that contains the preview stream within the preview control, when the scaling mode is Uniform
        /// </summary>
        /// <param name="previewResolution">The resolution at which the preview is running</param>
        /// <param name="previewControl">The control that is displaying the preview using Uniform as the scaling mode</param>
        /// <returns></returns>
        public Rect GetPreviewStreamRectInControl(VideoEncodingProperties previewResolution, CaptureElement previewControl)
        {
            var result = new Rect();

            // In case this function is called before everything is initialized correctly, return an empty result
            if (previewControl == null || previewControl.ActualHeight < 1 || previewControl.ActualWidth < 1 ||
                previewResolution == null || previewResolution.Height == 0 || previewResolution.Width == 0)
            {
                return result;
            }

            var streamWidth = previewResolution.Width;
            var streamHeight = previewResolution.Height;

            //For portrait orientations, the width and height need to be swapped
            if (_displayOrientation == DisplayOrientations.Portrait || _displayOrientation == DisplayOrientations.PortraitFlipped)
            {
                streamWidth = previewResolution.Height;
                streamHeight = previewResolution.Width;
            }

            // Start by assuming the preview display area in the control spans the entire width and height both (this is corrected in the next if for the necessary dimension)
            result.Width = previewControl.ActualWidth;
            result.Height = previewControl.ActualHeight;

            // If UI is "wider" than preview, letterboxing will be on the sides
            if ((previewControl.ActualWidth / previewControl.ActualHeight > streamWidth / (double)streamHeight))
            {
                var scale = previewControl.ActualHeight / streamHeight;
                var scaledWidth = streamWidth * scale;

                result.X = (previewControl.ActualWidth - scaledWidth) / 2.0;
                result.Width = scaledWidth;
            }
            else // Preview stream is "wider" than UI, so letterboxing will be on the top+bottom
            {
                var scale = previewControl.ActualWidth / streamWidth;
                var scaledHeight = streamHeight * scale;

                result.Y = (previewControl.ActualHeight - scaledHeight) / 2.0;
                result.Height = scaledHeight;
            }

            return result;
        }

        #endregion

        public async Task CaptureFaceToFileAsync(StorageFile photoFile, BitmapBounds faceBounds)
        {
            // Get video frame
            const int height = 480;
            double scale = height / (double)_previewProperties.Height;
            using (VideoFrame videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)(_previewProperties.Width * scale), height))
            //using (VideoFrame videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height))
            {
                await VideoCapture.GetPreviewFrameAsync(videoFrame);

                faceBounds = GetFaceBoundsFromFrame(faceBounds, 1);
                TryExtendFaceBounds(
                    videoFrame.SoftwareBitmap.PixelWidth,
                    videoFrame.SoftwareBitmap.PixelHeight,
                    Constants.FaceBoxRatio,
                    ref faceBounds);

                await SaveBoundedBoxToFileAsync(photoFile, videoFrame.SoftwareBitmap, BitmapEncoder.JpegEncoderId, faceBounds);
            }
        }

        private bool TryExtendFaceBounds(int imageWidth, int imageHeight, double ratio, ref BitmapBounds bounds)
        {
            // Get center of face
            var width = bounds.Width;
            var height = bounds.Height;
            var center = new Point(bounds.X + width / 2, bounds.Y + height / 2);

            // Get new bounds
            var newLeft = center.X - width * ratio / 2;
            var newRight = center.X + width * ratio / 2;
            var newTop = center.Y - width * ratio / 2;
            var newBottom = center.Y + width * ratio / 2;

            if (newLeft > 0 &&
                newRight < imageWidth &&
                newTop > 0 &&
                newBottom < imageHeight)
            {
                bounds.X = (uint)Math.Truncate(newLeft);
                bounds.Y = (uint)Math.Truncate(newTop);
                bounds.Height = (uint)Math.Truncate(height * ratio);
                bounds.Width = (uint)Math.Truncate(width * ratio);

                return true;
            }
            return false;
        }

        public async Task CaptureFaceToFileAsync(StorageFile photoFile)
        {
            // Get video frame
            const int height = 480;
            double scale = height / (double)_previewProperties.Height;

            // Capture gray image for face detection
            var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)(_previewProperties.Width * scale), height);
            var frame = await VideoCapture.GetPreviewFrameAsync(videoFrame);
            //Capture color image for saving            
            var grayVideoFrame = new VideoFrame(BitmapPixelFormat.Gray8, (int)(_previewProperties.Width * scale), height);
            await VideoCapture.GetPreviewFrameAsync(grayVideoFrame);
            // Detect faces
            IList<DetectedFace> faces = null;
            if (FaceDetector.IsBitmapPixelFormatSupported(grayVideoFrame.SoftwareBitmap.BitmapPixelFormat))
            {
                faces = await _faceTracker.ProcessNextFrameAsync(grayVideoFrame);
            }

            if ((faces ?? throw new InvalidOperationException()).Any())
            {
                var mainFace = faces.OrderByDescending(f => f.FaceBox.Height * f.FaceBox.Width).First();
                var faceBounds = GetFaceBoundsFromFrame(mainFace.FaceBox, 1);
                TryExtendFaceBounds(
                    videoFrame.SoftwareBitmap.PixelWidth,
                    videoFrame.SoftwareBitmap.PixelHeight,
                    Constants.FaceBoxRatio,
                    ref faceBounds);
                await SaveBoundedBoxToFileAsync(photoFile, frame.SoftwareBitmap, BitmapEncoder.BmpEncoderId, faceBounds);
            }
        }

        private BitmapBounds GetFaceBoundsFromFrame(BitmapBounds bounds, double scale = 1)
        {
            // Apply ratio if needed
            if (scale != 1)
            {
                bounds.X = (uint)Math.Truncate(bounds.X * scale);
                bounds.Y = (uint)Math.Truncate(bounds.Y * scale);
                bounds.Width = (uint)Math.Truncate(bounds.Width * scale);
                bounds.Height = (uint)Math.Truncate(bounds.Height * scale);
            }
            return bounds;
        }

        private static async Task SaveBoundedBoxToFileAsync(StorageFile photoFile, SoftwareBitmap softwareBitmap, Guid encoderId, BitmapBounds bounds)
        {
            using (IRandomAccessStream writeStream = await photoFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                try
                {
                    // Create an encoder with the desired format
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, writeStream);

                    encoder.SetSoftwareBitmap(softwareBitmap);

                    encoder.BitmapTransform.Bounds = bounds;
                    await encoder.FlushAsync();
                }
                catch (Exception)
                {
                    //TODO handle error
                }
            }
        }
    }
}
