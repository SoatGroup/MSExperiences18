using Microsoft.ProjectOxford.Face.Contract;

namespace FaceApiProxy
{
    public class FindSimilarFacesResponse
    {
        public string Error { get; set; }
        public SimilarPersistedFace[] SimilarFaces { get; set; }
    }
}