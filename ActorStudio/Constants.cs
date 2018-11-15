using System;

namespace ActorStudio
{
    public static class Constants
    {
        public const double ScreenRatio = 1.78;

        //Face API Client
        public const string AzureCelebFacesGroupFolder = "CelebritiesDataset";

        public const int BigFaceSizeThreshold = 20000;

        public const string FaceCatpureFileName = "photo";
        public const string FaceCatpureFileExtension = "jpg";
        public static string EmotionsResultImageFileName = "score.png";
        public const string LocalPersonFileExtension = "jpg";

        public const int WaitBeforeEmotionCaptureMsDelay = 3000;
        public const int WaitBetweenEmotionCaptureMsDelay = 2000;
    }
}
