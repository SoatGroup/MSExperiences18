using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ActorStudio
{
    public static class PicturesHelper
    {
        internal static async Task<StorageFolder> GetPersonFolderAsync(string groupFolderName)
        {
            return await KnownFolders.PicturesLibrary.GetFolderAsync(groupFolderName);
        }

        internal static async Task<IRandomAccessStream> GetPersonPictureAsync(string groupFolderName, string personName)
        {
            var personsFolder = await GetPersonFolderAsync(groupFolderName);
            var matchFile = await personsFolder.GetFileAsync($"{personName}.{Constants.LocalPersonFileExtension}");
            IRandomAccessStream photoStream = await matchFile.OpenReadAsync();
            return photoStream;
        }

        internal static async Task CleanFaceCapturesAsync()
        {
            var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
            var pattern = new Regex($@"\b{Constants.FaceCatpureFileName}(\s*\([0-9]*\)*.)*\.{Constants.FaceCatpureFileExtension}");
            var facePictures = files.Where(f => pattern.Match(f.Name).Success);
            foreach (var facePicture in facePictures) await facePicture.DeleteAsync();
        }

        internal static async Task<StorageFile> SaveResultBufferAsync(IBuffer pixelBuffer, int pixelWidth, int pixelHeight, float rawDpiX,
            float rawDpiY)
        {
            var pixels = pixelBuffer.ToArray();
            var file = await CreateFileAsync(Constants.EmotionsResultImageFileName, CreationCollisionOption.ReplaceExisting);
            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    (uint) pixelWidth,
                    (uint) pixelHeight,
                    rawDpiX,
                    rawDpiY,
                    pixels);
                await encoder.FlushAsync();
            }

            return file;
        }

        internal static Task<StorageFile> CreatePictureAsync()
        {
            return CreateFileAsync(Constants.FaceCatpureFileName + '.' + Constants.FaceCatpureFileExtension);
        }

        private static async Task<StorageFile> CreateFileAsync(string fileName,
            CreationCollisionOption collisionOption = CreationCollisionOption.GenerateUniqueName)
        {
            return await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, collisionOption);
        }
    }
}