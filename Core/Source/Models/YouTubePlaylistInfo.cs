namespace MasDev.YouTube.Model
{
    /// <summary>
    /// Represents a set of attributes owned by a Playlist
    /// </summary>
    public class YouTubePlaylistInfo : UniqueModel
    {
        public readonly string Id;
        public readonly IPagedAsyncEnumerable<YouTubeVideoInfo> Videos;

        internal YouTubePlaylistInfo(string playlistId, IPagedAsyncEnumerable<YouTubeVideoInfo> videos)
        {
            Id = playlistId;
            Videos = videos;
        }
    }
}