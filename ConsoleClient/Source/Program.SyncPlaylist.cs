using System;
using System.Threading.Tasks;
using MasDev.YouTube.Model;

namespace MasDev.YouTube.ConsoleClient
{
    public partial class Program
    {
        private static async Task SyncPlaylistAsync(YouTubeClient client, YouTubeVideoCollectionDownloadOptions downloadOptions, string playlistId)
        {
            Console.WriteLine($"Analyzing playlist [{playlistId}]");
            var playlistFeatures = client.Playlists;
            var analyzer = playlistFeatures.GetAnalyzer(playlistId);
            var analyzerResult = analyzer.Analyze(downloadOptions.DownloadFolder);

            var filesToSyncCount = 0;
            await analyzerResult
                .Where(a => !a.IsStored)
                .ForEach(a =>
                {
                    Console.WriteLine($"Video [{a.Video.Title}] is out of sync. Would you like to download it? Y/N");
                    var shouldSync = Console.ReadLine().Trim().ToLowerInvariant();
                    a.ShouldSync = shouldSync == "y" || shouldSync == "yes";
                    if (a.ShouldSync)
                        filesToSyncCount++;
                });

            if (filesToSyncCount == 0)
            {
                Console.WriteLine("\n\nNo fies to sync, press enter to exit");
                Console.ReadLine();
                return;
            }

            Console.WriteLine($"\n\n{filesToSyncCount} files to sync. Sync operation started");
            var syncronizer = analyzer.GetSyncronizer(analyzerResult);

            syncronizer.DownloadQueued += OnDownloadQueued;
            Console.WriteLine($"Syncronizing playlist with parallelism level [{downloadOptions.ParallelismLevel}] to folder [{downloadOptions.DownloadFolder}]\n\n");

            var looper = new Looper(350000);
            looper.Loop += LogPendingDownloads;
            looper.Start();

            await syncronizer.DownloadAsync(downloadOptions);
            looper.Stop();

            Console.WriteLine("\n\nPlaylist syncronized, press enter to exit");
            Console.ReadLine();
        }
    }
}
