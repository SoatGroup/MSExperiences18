using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;

namespace ActorStudio
{
    public static class PicturesHelper
    {
        private static readonly string[] DefaultMediaFileExtensions = {
            // picture
            ".jpg",
            ".png",
            ".bmp",
        };

        public static async Task<IRandomAccessStream> GetPersonPictureAsync(string groupFolderName, string personName)
        {
            var personsFolder = await KnownFolders.PicturesLibrary.GetFolderAsync(groupFolderName);
            var personFolder = await personsFolder.GetFolderAsync(personName);
            // Take first image in folder
            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, DefaultMediaFileExtensions);
            queryOptions.FolderDepth = FolderDepth.Shallow;
            var query = personFolder.CreateFileQueryWithOptions(queryOptions);
            var imgFiles = await query.GetFilesAsync();
            var matchFile = imgFiles.First();
            IRandomAccessStream photoStream = await matchFile.OpenReadAsync();
            return photoStream;
        }

        internal static async Task CleanFaceCapturesAsync()
        {
            var files = await KnownFolders.PicturesLibrary.GetFilesAsync();
            Regex pattern = new Regex(@"\bphoto(\s*\([0-9]*\)*.)*\.jpg\b");
            var facePictures = files.Where(f => pattern.Match(f.Name).Success);
            foreach (var facePicture in facePictures)
            {
                await facePicture.DeleteAsync();
            }
        }

        internal static async Task<StorageFile> SaveResultBufferAsync(IBuffer pixelBuffer, int pixelWidth, int pixelHeight, float rawDpiX, float rawDpiY)
        {
            var pixels = pixelBuffer.ToArray();
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("score" + ".png", CreationCollisionOption.ReplaceExisting);
            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                     BitmapAlphaMode.Premultiplied,
                                     (uint)pixelWidth,
                                     (uint)pixelHeight,
                                     rawDpiX,
                                     rawDpiY,
                                     pixels);
                await encoder.FlushAsync();
            }

            return file;
        }
    }
}
