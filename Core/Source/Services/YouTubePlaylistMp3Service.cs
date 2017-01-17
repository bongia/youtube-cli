using System.Collections.Generic;
using System.Net.Http;
using MasDev.YouTube.Model;

namespace MasDev.YouTube.Services
{
    internal class YouTubePlaylistMp3Service : IYouTubeDownloadService
    {
        const string DownloadVideoApiFormat = "http://youtubeplaylist-mp3.com/download/index/{0}";
        public string Extension { get; } = "mp3";
        public HttpMethod Method { get; } = HttpMethod.Get;
        public long? MinimumFileSize { get; } = 2048;

        public string GetDownloadUrl(YouTubeVideoInfo video)
        {
            return string.Format(DownloadVideoApiFormat, video.Id);
        }

        public IReadOnlyDictionary<string, string> Headers(YouTubeVideoInfo video)
        {
            return null;
        }
    }
}