using System.Threading.Tasks;
using MasDev.YouTube.Model;

namespace MasDev.YouTube.Download
{
    public delegate void DownloadQueuedHandler(YouTubeDownloadOperation video);

    /// <summary>
    /// This class can be used to download a collection of video
    /// </summary>
    public class YouTubeVideoCollectionDownloader : UniqueModel
    {
        /// <summary>
        /// This event is invoked when a DownloadOperation is queued in the download queue. The operation is not started yet.
        /// </summary>
        public event DownloadQueuedHandler DownloadQueued;

        /// <summary>
        /// The videos to download
        /// </summary>
        public readonly IPagedAsyncEnumerable<YouTubeVideoInfo> Videos;

        internal YouTubeVideoCollectionDownloader(IPagedAsyncEnumerable<YouTubeVideoInfo> pagedVideos)
        {
            Videos = pagedVideos;
        }

        public async Task DownloadAsync(YouTubeVideoCollectionDownloadOptions options)
        {
            options.Validate();

            var queue = new TaskQueue(options.ParallelismLevel);
            queue.ThrowOnTaskFailure = false;

            var pagedVideoEnumerator = Videos.GetEnumerator();
            while (await pagedVideoEnumerator.MoveNextAsync())
            {
                foreach (var video in pagedVideoEnumerator.Current)
                {
                    var videoHandler = options.Factory.CreateDownloadOperation(video, options);
                    var taskReference = videoHandler.TaskReference;
                    queue.Enqueue(taskReference);
                    DownloadQueued?.Invoke(videoHandler);
                }
                await queue.DequeueAsync();
            }
        }
    }
}