using MasDev.YouTube.Model;

namespace MasDev.YouTube.Download
{
    /// <summary>
    /// This class can be used to download a Playlist
    /// </summary>
    public class YouTubePlaylistDownloader : YouTubeVideoCollectionDownloader
    {
        /// <summary>
        /// The playlist to download
        /// </summary>
        public readonly YouTubePlaylistInfo Playlist;

        internal YouTubePlaylistDownloader(YouTubePlaylistInfo playlist) : base(playlist.Videos)
        {
            Playlist = playlist;
        }
    }
}