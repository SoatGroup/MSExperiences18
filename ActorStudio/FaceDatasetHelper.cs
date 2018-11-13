using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using FaceApiProxy;
using Microsoft.ProjectOxford.Face;

namespace ActorStudio
{
    internal static class FaceDatasetHelper
    {
        public static async Task<IdentifiedFace> CheckGroupAsync(FaceServiceClient faceClient, Stream stream, string personGroupId, string groupImagesFolder)
        {
            try
            {
                var response = await FaceApiHelper.IdentifyPersonAsync(faceClient, stream, personGroupId);

                if (response?.Candidates == null || response.Candidates.Length == 0)
                {
                    return null;
                }

                // Due to legal limitations, Face API does not support images retrieval in any circumstance currently.You need to store the images and maintain the relationship between face ids and images by yourself.
                var personsFolder = await PicturesHelper.GetPersonFolderAsync(groupImagesFolder);
                var dataSet = await faceClient.ListPersonsAsync(personGroupId);

                var matches =
                    from c in response.Candidates
                    join p in dataSet on c.PersonId equals p.PersonId into ps
                    from p in ps.DefaultIfEmpty()
                    select new IdentifiedFace
                    {
                        Confidence = c.Confidence,
                        PersonName = p == null ? "(No matching face)" : p.Name,
                        FaceId = c.PersonId
                    };

                var match = matches.OrderByDescending(m => m.Confidence).FirstOrDefault();


                if (match == null)
                {
                    return null;
                }

                var matchFile = await personsFolder.GetFileAsync($"{match.PersonName}.{Constants.LocalPersonFileExtension}");

                IRandomAccessStream photoStream = await matchFile.OpenReadAsync();
                match.FaceStream = photoStream.CloneStream().AsStream();
                return match;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
