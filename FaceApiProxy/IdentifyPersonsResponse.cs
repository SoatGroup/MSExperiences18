using Microsoft.ProjectOxford.Face.Contract;

namespace FaceApiProxy
{
    public class IdentifyPersonsResponse
    {
        public string Error { get; set; }
        public Candidate[] Candidates { get; set; }
    }
}