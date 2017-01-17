using MasDev.YouTube.Model;

namespace MasDev.YouTube.Features
{
    public class YouTubePlaylistAnalyzer : YouTubeVideoCollectionAnalyzer
    {
        public readonly YouTubePlaylistInfo Playlist;

        internal YouTubePlaylistAnalyzer(YouTubePlaylistInfo playlist) : base(playlist.Videos)
        {
            Playlist = playlist;
        }
    }
}