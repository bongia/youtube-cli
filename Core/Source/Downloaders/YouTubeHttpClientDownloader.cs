using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using MasDev.YouTube.Model;
using MasDev.YouTube.Services;

namespace MasDev.YouTube.Download
{
    /// <summary>
    /// An implementation of YouTubeDownloadOperation based on HttpClient
    /// </summary>
    public class YouTubeHttpClientDownloadOperation : YouTubeDownloadOperation
    {
        internal YouTubeHttpClientDownloadOperation(YouTubeVideoInfo info, YouTubeDownloadOptions options) : base(info, options)
        {
        }

        protected override async Task DownloadAsync(IYouTubeDownloadService service, string localPath)
        {
            using (var httpClient = new HttpClient())
            {
                var downloadUrl = service.GetDownloadUrl(Video);

                // TODO headers, HTTP Method

                var mp3Stream = await httpClient.GetStreamAsync(downloadUrl);
                long? mp3TotalSize = null;
                var mp3SizeSoFar = 0;

                if (mp3Stream.CanSeek)
                    mp3TotalSize = mp3Stream.Length + 1;

                using (var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write))
                {
                    var buffer = new byte[10240];
                    var lastChunkBytesRead = 0;
                    var stopWatch = Stopwatch.StartNew();

                    while ((lastChunkBytesRead = await mp3Stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        mp3SizeSoFar += lastChunkBytesRead;
                        fileStream.Write(buffer, 0, lastChunkBytesRead);

                        var completionPercentage = GetCompletionPercentage(mp3SizeSoFar, mp3TotalSize);
                        var elapsedSeconds = (stopWatch.ElapsedMilliseconds + 1) / 1000m;
                        var speed = (lastChunkBytesRead / 1024) / elapsedSeconds;

                        PublishDownloadProgress(completionPercentage, speed);
                        stopWatch.Restart();
                    }
                }
            }
        }

        private decimal? GetCompletionPercentage(int mp3SizeSoFar, long? mp3TotalSize)
        {
            if (mp3TotalSize == null)
                return null;
            return (mp3SizeSoFar * 100m) / mp3TotalSize.Value;
        }
    }

    /// <summary>
    /// A factory that yields DownloadOperations based on HttpClient
    /// </summary>
    public class YouTubeHttpClientDownloadOperationFactory : IYouTubeDownloadOperationFactory
    {
        public static readonly YouTubeHttpClientDownloadOperationFactory Instance = new YouTubeHttpClientDownloadOperationFactory();

        private YouTubeHttpClientDownloadOperationFactory()
        {
            // singleton
        }

        public YouTubeDownloadOperation CreateDownloadOperation(YouTubeVideoInfo info, YouTubeDownloadOptions options)
        {
            return new YouTubeHttpClientDownloadOperation(info, options);
        }
    }
}