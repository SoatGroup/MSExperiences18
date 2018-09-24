using Microsoft.ProjectOxford.Face.Contract;

namespace FaceApiProxy
{
    public class TrainedFace
    {
        public Face Face { get; }
        public string PersonName { get; }
        public string FilePath { get; }

        public TrainedFace(Face face, string personName, string filePath)
        {
            this.Face = face;
            this.PersonName = personName;
            this.FilePath = filePath;
        }
    }
}