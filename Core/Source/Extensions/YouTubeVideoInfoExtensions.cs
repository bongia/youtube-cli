using System.Threading.Tasks;
using MasDev.YouTube.Download;
using MasDev.YouTube.Model;

namespace MasDev.YouTube.Extensions
{
    public static class YouTubeVideoInfoExtensions
    {
        public static async Task DownloadAsync(this YouTubeVideoInfo video, IYouTubeDownloadOperationFactory operationFactory, YouTubeDownloadOptions options)
        {
            var downloader = operationFactory.CreateDownloadOperation(video, options);
            var taskCompletionSource = new TaskCompletionSource<object>();
            downloader.Error += (s, e) => taskCompletionSource.SetException(e);
            downloader.Success += (s, f, sp) => taskCompletionSource.SetResult(null);
            await downloader.DownloadAsync();
            await taskCompletionSource.Task;
        }
    }
}