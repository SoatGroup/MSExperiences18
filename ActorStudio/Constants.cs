namespace ActorStudio
{
    public static class Constants
    {
        public const double ScreenRatio = 1.78;

        //Face API Client
        public const string AzureFaceApiKey = "34f95dfe9ef7460e9bfbd19987a5b6c3";
        public const string AzureFaceApiRoot = "https://westeurope.api.cognitive.microsoft.com/face/v1.0";
        public const string AzureFacesListId = "124378ef-08e4-4854-8ff4-cadb32b51fc9";
        public const string AzureCelebFacesListName = "Game Of Thrones";
        public const string AzureCelebFacesGroupFolder = "Game Of Thrones";

        public const int BigFaceSizeThreshold = 20000;

        public const string FaceCatpureFileName = "photo";
        public const string FaceCatpureFileExtension = "jpg";
        public static string EmotionsResultImageFileName = "score.png";
        public const string LocalPersonFileExtension = "jpg";

        public const int WaitBeforeEmotionCaptureMsDelay = 3000;
        public const int WaitBetweenEmotionCaptureMsDelay = 2000;

        public const int CameraIndex = 0;
    }
}
