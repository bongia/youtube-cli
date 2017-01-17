using System.Collections.Generic;
using System.Net.Http;
using MasDev.YouTube.Model;

namespace MasDev.YouTube.Services
{
    /// <summary>
    ///  It describes a remote service that can perform download operations on a specific feature
    /// </summary>
    public interface IYouTubeDownloadService
    {
        string GetDownloadUrl(YouTubeVideoInfo video);
        IReadOnlyDictionary<string, string> Headers(YouTubeVideoInfo video);
        HttpMethod Method { get; }
        string Extension { get; }
        long? MinimumFileSize { get; }
    }

    /// <summary>
    ///  It gives some predefined download services
    /// </summary>
    public static class DefaultYouTubeDownloadServices
    {
        public static readonly IReadOnlyList<IYouTubeDownloadService> Audio;
        public static readonly IReadOnlyList<IYouTubeDownloadService> Video;

        static DefaultYouTubeDownloadServices()
        {
            Audio = new List<IYouTubeDownloadService>
            {
                new YouTubePlaylistMp3Service(),
                // TODO altri
            }.AsReadOnly();

            Video = new List<IYouTubeDownloadService>
            {
                // TODO altri
            }.AsReadOnly();
        }
    }
}