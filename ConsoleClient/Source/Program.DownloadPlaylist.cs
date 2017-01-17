using System;
using System.Threading.Tasks;
using MasDev.YouTube.Model;

namespace MasDev.YouTube.ConsoleClient
{
    public partial class Program
    {
        private static async Task DownloadPlaylistAsync(YouTubeClient client, YouTubeVideoCollectionDownloadOptions downloadOptions, string playlistId)
        {
            Console.WriteLine($"Fetching playlist [{playlistId}]");
            var playlistFeatures = client.Playlists;
            var playlistDownloader = playlistFeatures.GetDownloadManager(playlistId);
            playlistDownloader.DownloadQueued += OnDownloadQueued;

            Console.WriteLine($"Fetched playlist [{playlistDownloader.Playlist.Id}]");
            Console.WriteLine($"Downloading playlist with parallelism level [{downloadOptions.ParallelismLevel}] to folder [{downloadOptions.DownloadFolder}]\n\n");

            var looper = new Looper(350000);
            looper.Loop += LogPendingDownloads;
            looper.Start();

            await playlistDownloader.DownloadAsync(downloadOptions);
            looper.Stop();

            Console.WriteLine("\n\nPlaylist downloaded, press enter to exit");
            Console.ReadLine();
        }
    }
}
