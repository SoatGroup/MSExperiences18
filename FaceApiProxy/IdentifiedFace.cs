using System;
using System.IO;

namespace FaceApiProxy
{
    public class IdentifiedFace
    {
        public double Confidence { get; set; }
        public string PersonName { get; set; }
        public Guid FaceId { get; set; }
        public Stream FaceStream { get; set; }
    }
}
