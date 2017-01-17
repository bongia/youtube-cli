using MasDev.YouTube.Model;

namespace MasDev.YouTube.Download
{
    /// <summary>
    /// A factory that yields a YouTubeDownloadOperation related to a Video and configured with a given YouTubeDownloadOptions
    /// </summary>
    public interface IYouTubeDownloadOperationFactory
    {
        YouTubeDownloadOperation CreateDownloadOperation(YouTubeVideoInfo info, YouTubeDownloadOptions options);
    }
}