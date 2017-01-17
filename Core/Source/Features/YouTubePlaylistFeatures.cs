using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MasDev.YouTube.Download;
using MasDev.YouTube.Model;

namespace MasDev.YouTube.Features
{
    /// <summary>
    /// Gives access to a set of operations that can be performed on a Playlist
    /// </summary>
    public class YouTubePlaylistFeatures : YouTubeClientFeature
    {
        const string GetPlaylistItemUrl = "playlistItems";

        internal YouTubePlaylistFeatures(YouTubeClient client) : base(client)
        {
        }

        /// <summary>
        /// Prepares a download manager for the Playlist with the given Id
        /// </summary>
        public YouTubePlaylistDownloader GetDownloadManager(string playlistId)
        {
            var playlist = GetPlaylist(playlistId);
            return new YouTubePlaylistDownloader(playlist);
        }

        /// <summary>
        /// Prepares an analyzer for the Playlist with the given Id
        /// </summary>
        public YouTubePlaylistAnalyzer GetAnalyzer(string playlistId)
        {
            var playlist = GetPlaylist(playlistId);
            return new YouTubePlaylistAnalyzer(playlist);
        }

        private YouTubePlaylistInfo GetPlaylist(string playlistId)
        {
            // TODO cache?
            var videos = new PagedVideoAsyncEnumerator(playlistId, Client).Select(p => p.Videos);
            return new YouTubePlaylistInfo(playlistId, videos.AsPaged());
        }

        class PagedVideoAsyncEnumerator : AsyncEnumerable<PagedVideoAsyncEnumerator.VideoPage>
        {
            private readonly string _playlistId;
            private readonly YouTubeClient _client;

            public PagedVideoAsyncEnumerator(string playlistId, YouTubeClient client)
            {
                _playlistId = playlistId;
                _client = client;
            }

            protected override async Task<VideoPage> MoveNextAsync(VideoPage previous, int iterationIndex)
            {
                if (previous != null && previous.NextPageToken == null)
                    throw new IterationFinishedException();

                var playlistPage = await GetPlaylistItemsPageAsync(_playlistId, previous == null ? null : previous.NextPageToken);
                var videos = new List<YouTubeVideoInfo>();
                var nextPageToken = ParsePage(videos, playlistPage);

                return new VideoPage
                {
                    NextPageToken = nextPageToken,
                    Videos = videos.Where(v => v != null).ToList().AsReadOnly()
                };
            }

            private string ParsePage(IList<YouTubeVideoInfo> videos, dynamic playlistPage)
            {
                var items = playlistPage["items"];
                foreach (var item in items)
                {
                    var snippet = item["snippet"];
                    var thumbnails = snippet["thumbnails"];

                    YouTubeVideoInfo.Thumbnail[] videoThumbnails = null;
                    if (thumbnails != null)
                    {
                        videoThumbnails = new YouTubeVideoInfo.Thumbnail[5];
                        videoThumbnails[0] = GetThumbnail(thumbnails["default"]);
                        videoThumbnails[1] = GetThumbnail(thumbnails["high"]);
                        videoThumbnails[2] = GetThumbnail(thumbnails["maxres"]);
                        videoThumbnails[3] = GetThumbnail(thumbnails["medium"]);
                        videoThumbnails[4] = GetThumbnail(thumbnails["standard"]);
                    }

                    videos.Add(new YouTubeVideoInfo
                    {
                        ChannelId = snippet["channelId"],
                        Channel = snippet["channelTitle"],
                        Description = snippet["description"],
                        Title = snippet["title"],
                        Id = snippet["resourceId"]["videoId"],
                        Thumbnails = videoThumbnails,
                    });
                }

                try
                {
                    return playlistPage["nextPageToken"];
                }
                catch
                {
                    return null;
                }
            }

            private YouTubeVideoInfo.Thumbnail GetThumbnail(dynamic thumbnail)
            {
                return thumbnail == null ? null : new YouTubeVideoInfo.Thumbnail
                {
                    Url = thumbnail["url"],
                    Width = thumbnail["width"],
                    Height = thumbnail["height"]
                };
            }

            private async Task<dynamic> GetPlaylistItemsPageAsync(string playlistId, string nextPageToken)
            {
                var parameters = new Params
                 {
                    new Param("part", "snippet"),
                    new Param("maxResults", "50"),
                    new Param("playlistId", playlistId)
                };

                if (nextPageToken != null)
                    parameters.Add("pageToken", nextPageToken);

                return await _client.GetAsync(GetPlaylistItemUrl, parameters);
            }

            internal class VideoPage
            {
                public IReadOnlyList<YouTubeVideoInfo> Videos;
                public string NextPageToken;
            }
        }
    }
}