using FaceApiProxy;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;

namespace FaceControls
{
    internal static class FaceDatasetHelper
    {
        private static readonly string[] DefaultMediaFileExtensions = {
            // picture
            ".jpg",
            ".png",
            ".bmp",
        };

        public static async Task<IEnumerable<TrainedFace>> UploadFacesAsync(FaceServiceClient faceClient, string facesFolder, string facesListId, string facesListName, int imagesCount = 1, bool fromInfoFile = false, string[] mediaFileExtensions = null)
        {
            if (mediaFileExtensions == null)
            {
                mediaFileExtensions = DefaultMediaFileExtensions;
            }

            try
            {
                await faceClient.GetLargeFaceListAsync(facesListId);
            }
            catch (FaceAPIException ex)
            {
                if (ex.ErrorCode == "LargeFaceListNotFound")
                {
                    await faceClient.CreateLargeFaceListAsync(facesListId, facesListName,
                        "Face list for sample");
                }
            }

            var uploadTasks = new List<Task<List<TrainedFace>>>();
            var largeFacesFolder = await KnownFolders.PicturesLibrary.GetFolderAsync(facesFolder);
            var folders = await largeFacesFolder.GetFoldersAsync();
            foreach (var folder in folders)
            {
                if (fromInfoFile)
                { // Read info files with links
                    var infoFile = await folder.GetFileAsync("info.txt");
                    var infoContent = await FileIO.ReadTextAsync(infoFile);
                    string pattern = @"[0-9]+\.jpg[ \t]([a-z0-9A-Z\/\.\:_\-]+)";

                    IEnumerable<Match> matches = Regex.Matches(infoContent, pattern).ToList();
                    if (imagesCount > 0)
                    {
                        matches = matches.Take(imagesCount);
                    }

                    foreach (Match match in matches)
                    {
                        var faceLink = match.Groups[1].Value;
                        var uploadTask = FaceApiHelper.UploadFaceAsync(faceClient, facesListId, folder.Name, faceLink);
                        uploadTasks.Add(uploadTask);
                    }
                }
                else
                {
                    var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, mediaFileExtensions);
                    queryOptions.FolderDepth = FolderDepth.Shallow;

                    var query = folder.CreateFileQueryWithOptions(queryOptions);
                    var imgFiles = await query.GetFilesAsync();
                    foreach (var imgFile in imgFiles)
                    {
                        // Upload load
                        using (var stream = await imgFile.OpenReadAsync())
                        {
                            var uploadTask = FaceApiHelper.UploadFaceAsync(faceClient, facesListId, folder.Name, stream.CloneStream().AsStream(), imgFile.Path);
                            uploadTasks.Add(uploadTask);
                        }
                    }
                }
            }

            await Task.WhenAll(uploadTasks.ToArray());
            return uploadTasks.SelectMany(t => t.Result);
        }

        public static async Task WriteDatasetAsync(IEnumerable<TrainedFace> trainedFaces, string facesListName)
        {
            var datasetFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                $"dataset_{facesListName}.txt",
                CreationCollisionOption.ReplaceExisting);//Replace file at each upload
            var lines = trainedFaces.Select(tf => $"{tf.PersonName}\t{tf.Face.FaceId}\t{tf.FilePath}");
            await FileIO.WriteLinesAsync(datasetFile, lines);
        }

        public static async Task LoadDatasetAsync(ObservableCollection<TrainedFace> facesCollection, string facesListName)
        {
            var datasetFile = await KnownFolders.PicturesLibrary.GetFileAsync($"dataset_{facesListName}.txt");
            var lines = await FileIO.ReadLinesAsync(datasetFile);

            foreach (var line in lines)
            {
                var personName = line.Split('\t')[0];
                var persistedFaceId = line.Split('\t')[1];
                var filePath = line.Split('\t')[2];

                facesCollection.Add(new TrainedFace(
                    new Face { FaceId = new Guid(persistedFaceId) },
                    personName,
                    filePath
                ));
            }
        }
        
        public static async Task<bool> UploadPersonFaceAsync(FaceServiceClient faceClient, string personGroupId, Guid personId, string faceLink)
        {
            bool rateLimitExceeded;
            do
            {
                rateLimitExceeded = false;
                try
                {
                    AddPersistedFaceResult uploadedFace =
                        await faceClient.AddPersonFaceAsync(personGroupId, personId, faceLink, userData: faceLink);
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
                        return false;
                    }
                }
            } while (rateLimitExceeded == true);

            return true;
        }

        public static async Task<bool> UploadPersonFaceAsync(FaceServiceClient faceClient, string personGroupId, Guid personId, StorageFile faceFile)
        {
            bool rateLimitExceeded;
            do
            {
                rateLimitExceeded = false;
                try
                {
                    var stream = faceFile.OpenReadAsync();
                    using (IRandomAccessStreamWithContentType imageFileStream = await faceFile.OpenReadAsync())
                    {
                        AddPersistedFaceResult uploadedFace =
                            await faceClient.AddPersonFaceAsync(personGroupId, personId, imageFileStream.AsStream());
                    }
                }
                catch (Microsoft.ProjectOxford.Face.FaceAPIException e)
                {
                    if (e.ErrorCode == "RateLimitExceeded" ||
                        e.ErrorMessage.Contains("There is a conflict operation on resource"))
                    {
                        rateLimitExceeded = true;
                        await Task.Delay(100);
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            } while (rateLimitExceeded == true);

            return true;
        }
    }
}
