using System.IO;

namespace MasDev.YouTube.Model
{

    public class YouTubeVideoAnalysis : UniqueModel
    {
        public readonly YouTubeVideoInfo Video;
        public FileInfo LocalFile { get; internal set; }
        public bool IsStored { get { return LocalFile != null && LocalFile.Exists; } }

        bool? _shouldSync;
        public bool ShouldSync
        {
            get { return _shouldSync != null ? _shouldSync.Value : !IsStored; }
            set { _shouldSync = value; }
        }

        internal YouTubeVideoAnalysis(YouTubeVideoInfo video)
        {
            Video = video;
        }
    }
}