﻿namespace AzureSkyMedia.PlatformServices
{
    public struct MediaInsight
    {
        public string ProcessorId { get; set; }

        public string ProcessorName { get; set; }

        public string DocumentId { get; set; }

        public string SourceUrl { get; set; }
    }

    public class VideoIndexer
    {
        public VideoIndexer()
        {
            LanguageId = string.Empty;
            SearchPartition = string.Empty;
            VideoDescription = string.Empty;
            VideoMetadata = string.Empty;
            VideoPublic = false;
            AudioOnly = false;
        }

        public string LanguageId { get; set; }

        public string SearchPartition { get; set; }

        public string VideoDescription { get; set; }

        public string VideoMetadata { get; set; }

        public bool VideoPublic { get; set; }

        public bool AudioOnly { get; set; }
    }
}