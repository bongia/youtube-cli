using MasDev.YouTube.Model;

namespace MasDev.YouTube
{
    /// <summary>
    ///  Represents a set of operation to perform on a specific feature
    /// </summary>
    public abstract class YouTubeClientFeature : UniqueModel
    {
        protected readonly YouTubeClient Client;

        protected YouTubeClientOptions Options { get { return Client.Options; } }

        protected YouTubeClientFeature(YouTubeClient client)
        {
            Client = client;
        }
    }
}