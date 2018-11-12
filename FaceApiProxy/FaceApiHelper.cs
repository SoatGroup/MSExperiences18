using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace FaceApiProxy
{
    public static class FaceApiHelper
    {
        #region Faces

        /// <summary>
        /// Add face to a person in FaceAPI from an URL
        /// </summary>
        /// <param name="faceClient"></param>
        /// <param name="facesListId"></param>
        /// <param name="personName"></param>
        /// <param name="faceLink"></param>
        /// <returns></returns>
        public static async Task<List<TrainedFace>> UploadFaceAsync(FaceServiceClient faceClient, string facesListId, string personName, string faceLink)
        {
            var persistedFaces = new List<TrainedFace>();
            try
            {
                bool rateLimitExceeded;
                do
                {
                    rateLimitExceeded = false;
                    try
                    {
                        AddPersistedFaceResult uploadedFace = await faceClient.AddFaceToLargeFaceListAsync(facesListId, faceLink, userData: faceLink);

                        persistedFaces.Add(new TrainedFace(
                            new Face
                            {
                                FaceId = uploadedFace.PersistedFaceId
                            },
                            personName,
                            faceLink));
                    }
                    catch (FaceAPIException e)
                    {
                        if (e.ErrorCode == "RateLimitExceeded")
                        {
                            rateLimitExceeded = true;
                            await Task.Delay(1);
                        }
                        else if (e.ErrorCode != "InvalidURL" && e.ErrorCode != "InvalidImage")
                        {
                            throw;
                        }
                        // otherwise, just ignore this image
                    }
                } while (rateLimitExceeded);

                return persistedFaces;
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e);
                // just ignore this face
                return null;
            }
        }
        #endregion Person

        #region Persons

        /// <summary>
        /// Add face to a person in FaceAPI from a stream
        /// </summary>
        /// <param name="faceClient"></param>
        /// <param name="facesListId"></param>
        /// <param name="personName"></param>
        /// <param name="faceStream"></param>
        /// <param name="filePath">Local file path</param>
        /// <returns></returns>
        public static async Task<List<TrainedFace>> UploadFaceAsync(FaceServiceClient faceClient, string facesListId, string personName, Stream faceStream, string filePath)
        {
            var persistedFaces = new List<TrainedFace>();
            try
            {
                bool rateLimitExceeded;
                do
                {
                    rateLimitExceeded = false;
                    try
                    {
                        AddPersistedFaceResult uploadedFace = await faceClient.AddFaceToLargeFaceListAsync(facesListId, faceStream, personName);

                        persistedFaces.Add(new TrainedFace(
                            new Face
                            {
                                FaceId = uploadedFace.PersistedFaceId
                            },
                            personName,
                            filePath));
                    }
                    catch (FaceAPIException e)
                    {
                        if (e.ErrorCode == "RateLimitExceeded")
                        {
                            rateLimitExceeded = true;
                            await Task.Delay(1);
                        }
                        else if (e.ErrorCode != "InvalidURL" && e.ErrorCode != "InvalidImage")
                        {
                            throw;
                        }
                        // otherwise, just ignore this image
                    }
                } while (rateLimitExceeded);

                return persistedFaces;
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e);
                // just ignore this face
                return null;
            }
        }

        /// <summary>
        /// Find similar person in a face list from a stream source
        /// </summary>
        /// <param name="faceClient"></param>
        /// <param name="stream"></param>
        /// <param name="faceGroupId"></param>
        /// <param name="confidenceThreshold"></param>
        /// <param name="requestCandidatesCount"></param>
        /// <returns></returns>
        public static async Task<IdentifyPersonsResponse> IdentifyPersonAsync(FaceServiceClient faceClient, Stream stream, string faceGroupId, float confidenceThreshold = 0, int requestCandidatesCount = 4)
        {
            var response = new IdentifyPersonsResponse();

            try
            {
                var faces = await faceClient.DetectAsync(stream);

                // Select biggest face
                var face = faces.OrderByDescending(f => f.FaceRectangle.Height * f.FaceRectangle.Width).FirstOrDefault();
                if (face == null)
                {
                    return null;
                }
                var faceIds = new[] { face.FaceId };

                // Call find facial match similar REST API, the result faces the top N with the highest similar confidence 
                var identifyResults = await faceClient.IdentifyAsync(faceGroupId, faceIds, confidenceThreshold, requestCandidatesCount);
                if (identifyResults.Length >= 0)
                {
                    var result = identifyResults.First(r => r.FaceId == face.FaceId);
                    response.Candidates = result.Candidates;
                }
                else
                {
                    response.Error = "Response: No matching person.";
                }
            }
            catch (FaceAPIException ex)
            {
                response.Error = $"Response: {ex.ErrorCode}. {ex.ErrorMessage}";
            }

            return response;
        }
        #endregion Person

        public static async Task<FaceAttributes> DetectEmotionsAsync(FaceServiceClient faceClient, Stream faceStream, FaceAttributeType[] requiredFaceAttributes)
        {
            var response = await faceClient.DetectAsync(faceStream, true, true, requiredFaceAttributes);
            return response?.FirstOrDefault()?.FaceAttributes;
        }
    }
}
