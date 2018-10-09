using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
