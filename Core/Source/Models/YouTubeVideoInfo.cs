namespace MasDev.YouTube.Model
{
    /// <summary>
    /// Represents a set of attributes owned by a Video
    /// </summary>
    public class YouTubeVideoInfo : UniqueModel
    {
        public string Title { get; internal set; }
        public string Channel { get; internal set; }
        public string ChannelId { get; internal set; }
        public string Description { get; internal set; }
        public string Id { get; internal set; }
        public string PlaylistItemId { get; internal set; }
        public Thumbnail[] Thumbnails { get; internal set; }

        public class Thumbnail
        {
            public string Url { get; internal set; }
            public int Width { get; internal set; }
            public int Height { get; internal set; }
        }
    }
}