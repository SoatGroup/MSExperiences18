using System;
using Windows.Media.MediaProperties;

namespace FaceControls
{
    /// <summary>
    /// Wrapper class around IMediaEncodingProperties to help how devices report supported resolutions
    /// </summary>
    class StreamResolution
    {
        public IMediaEncodingProperties EncodingProperties { get; }

        public StreamResolution(IMediaEncodingProperties properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            // Only handle ImageEncodingProperties and VideoEncodingProperties, which are the two types that GetAvailableMediaStreamProperties can return
            if (!(properties is ImageEncodingProperties) && !(properties is VideoEncodingProperties))
            {
                throw new ArgumentException("Argument is of the wrong type. Required: " + typeof(ImageEncodingProperties).Name
                    + " or " + typeof(VideoEncodingProperties).Name + ".", nameof(properties));
            }

            // Store the actual instance of the IMediaEncodingProperties for setting them later
            EncodingProperties = properties;
        }

        public uint Width
        {
            get
            {
                if (EncodingProperties is ImageEncodingProperties properties)
                {
                    return properties.Width;
                }

                if (EncodingProperties is VideoEncodingProperties encodingProperties)
                {
                    return encodingProperties.Width;
                }

                return 0;
            }
        }

        public uint Height
        {
            get
            {
                if (EncodingProperties is ImageEncodingProperties properties)
                {
                    return properties.Height;
                }

                if (EncodingProperties is VideoEncodingProperties encodingProperties)
                {
                    return encodingProperties.Height;
                }

                return 0;
            }
        }

        public uint FrameRate
        {
            get
            {
                if (EncodingProperties is VideoEncodingProperties properties)
                {
                    if (properties.FrameRate.Denominator != 0)
                    {
                        return properties.FrameRate.Numerator / properties.FrameRate.Denominator;
                    }
                }

                return 0;
            }
        }

        public double AspectRatio => Math.Round((Height != 0) ? (Width / (double)Height) : double.NaN, 2);
    }
}