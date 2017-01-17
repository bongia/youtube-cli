using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MasDev.YouTube.Download;
using MasDev.YouTube.Model;
using MasDev.YouTube.Services;

namespace MasDev.YouTube.ConsoleClient
{
    public partial class Program
    {
        private const string ApiKey = "TODO";
        private static readonly IDictionary<Guid, YouTubeDownloadOperation> _pendingDownloads = new Dictionary<Guid, YouTubeDownloadOperation>();

        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            var parallelismLevel = 10;
            var downloadFolder = "Downloads";

            // var playlistId = "PL1HChj_66u30uPc2CkTqcZbU7tt3BRx06"; // GalaxyMusic - all
            // var playlistId = "PLx_tr69QV8CCS8NF-UKCclTplK9CjVmns"; // Spinning records - progressive
            // var playlistId = "PLx_tr69QV8CDRoIN45uZ8tEP_py52S5nZ"; // Spinning records - EDM
            var playlistId = "PLw-VjHDlEOgtUxngnrKzkDXlIYMU7h6NW"; // Majestic Casual - Majestic Color

            var clientOptions = new YouTubeClientOptions(ApiKey);
            var downloadOptions = new YouTubeVideoCollectionDownloadOptions(downloadFolder)
            {
                DownloadStrategy = YouTubeDownloadStrategy.DoNotDownloadIfExisting,
                ParallelismLevel = parallelismLevel,
                Factory = YouTubeHttpClientDownloadOperationFactory.Instance,
                Services = DefaultYouTubeDownloadServices.Audio
            };

            using (var client = new YouTubeClient(clientOptions))
            {
                await DownloadPlaylistAsync(client, downloadOptions, playlistId);
                // await SyncPlaylistAsync(client, downloadOptions, playlistId);
            }
        }

        private static void LogPendingDownloads(TimeSpan elapsed)
        {
            lock (_pendingDownloads)
            {
                System.Console.WriteLine($"Pending downloads:");
                if (_pendingDownloads.Count == 0)
                {
                    System.Console.WriteLine("\tNone");
                    return;
                }

                var index = 0;
                foreach (var pendingDownload in _pendingDownloads.Values)
                    System.Console.WriteLine($"\t{++index}: {pendingDownload.Video.Title}");
            }
        }

        private static void OnDownloadQueued(YouTubeDownloadOperation downloader)
        {
            ConsoleLogger.Attach(downloader, _pendingDownloads);
        }
    }
}
