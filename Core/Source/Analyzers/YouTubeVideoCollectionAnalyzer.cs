using System.Collections.Generic;
using System.IO;
using System.Linq;
using MasDev.YouTube.Download;
using MasDev.YouTube.Model;

namespace MasDev.YouTube.Features
{
    public class YouTubeVideoCollectionAnalyzer
    {
        public readonly IPagedAsyncEnumerable<YouTubeVideoInfo> Videos;

        internal YouTubeVideoCollectionAnalyzer(IPagedAsyncEnumerable<YouTubeVideoInfo> videos)
        {
            Videos = videos;
        }

        public IAsyncEnumerable<YouTubeVideoAnalysis> Analyze(string localPath)
        {
            if (!Directory.Exists(localPath))
                Directory.CreateDirectory(localPath);
            return Videos.SelectMany(v => Analyze(v, localPath));
        }

        private YouTubeVideoAnalysis Analyze(YouTubeVideoInfo video, string localPath)
        {
            var directoryFiles = Directory.EnumerateFiles(localPath);
            var result = new YouTubeVideoAnalysis(video);
            result.LocalFile = FindLocalFile(directoryFiles, video);
            return result;
        }

        private FileInfo FindLocalFile(IEnumerable<string> directoryFiles, YouTubeVideoInfo video)
        {
            var files = directoryFiles.Select(f => new FileInfo(f));
            return files.FirstOrDefault(f => IsVideoSavedToLocalFile(f, video));
        }

        private bool IsVideoSavedToLocalFile(FileInfo localFile, YouTubeVideoInfo video)
        {
            var fileNameWithExtension = localFile.Name;
            var cleanedName = Path.GetFileNameWithoutExtension(fileNameWithExtension).Trim();
            return cleanedName == video.Title;
        }

        public YouTubeVideoCollectionDownloader GetSyncronizer(IAsyncEnumerable<YouTubeVideoAnalysis> analysisResult)
        {
            var videos = analysisResult
                .Where(a => a.ShouldSync)
                .Select(a => a.Video)
                .FoldLeft(new List<YouTubeVideoInfo>())
                .Select(a => a.AsReadOnly())
                .AsPaged();
            return new YouTubeVideoCollectionDownloader(videos);
        }
    }
}