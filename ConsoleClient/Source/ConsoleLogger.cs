using System;
using System.Collections.Generic;
using MasDev.YouTube.Download;

namespace MasDev.YouTube.ConsoleClient
{
    public class ConsoleLogger
    {
        private static readonly object _lock = new object();

        public static void Attach(YouTubeDownloadOperation downloader, IDictionary<Guid, YouTubeDownloadOperation> queue = null)
        {
            new ConsoleLogger(downloader, queue);
        }

        private readonly IDictionary<Guid, YouTubeDownloadOperation> _queue;

        private ConsoleLogger(YouTubeDownloadOperation downloader, IDictionary<Guid, YouTubeDownloadOperation> queue = null)
        {
            _queue = queue;
            downloader.Error += OnDownloadError;
            downloader.Finish += OnDownloadFinished;
            downloader.ProgressChange += OnDownloadProgressChange;
            downloader.Start += OnDownloadStarted;
            downloader.Success += OnDownloadSuccess;
        }

        private void OnDownloadStarted(YouTubeDownloadOperation sender)
        {
            LockingQueue(q => q.Add(sender.GeneratedId, sender));
            WriteLine($"[{sender.Video.Title}] started", ConsoleColor.DarkGray);
        }

        private void OnDownloadProgressChange(YouTubeDownloadOperation sender, decimal? completionPercentage, decimal? speedInKb)
        {
            // Do nothing 
        }

        private void OnDownloadError(YouTubeDownloadOperation sender, Exception exception)
        {
            WriteLine($"[{sender.Video.Title}] failed due to [{exception.Message}]", ConsoleColor.Red);
        }

        private void OnDownloadSuccess(YouTubeDownloadOperation sender, string savedFile, decimal? averageSpeedInKb)
        {
            var formattedSpeed = averageSpeedInKb.HasValue ?
                (averageSpeedInKb.Value / 1024).ToString("N2") :
                "N/A";
            WriteLine($"[{sender.Video.Title}] @[{formattedSpeed}]Mbps saved to [{savedFile}]", ConsoleColor.Gray);
        }

        private void OnDownloadFinished(YouTubeDownloadOperation sender)
        {
            LockingQueue(q => q.Remove(sender.GeneratedId));
        }

        private void LockingQueue(Action<IDictionary<Guid, YouTubeDownloadOperation>> action)
        {
            if (_queue != null)
            {
                lock (_queue)
                    action(_queue);
            }
        }

        private static void WriteLine(string message, ConsoleColor foreColor)
        {
            lock (_lock)
            {
                var previousColor = Console.ForegroundColor;
                Console.ForegroundColor = foreColor;
                Console.WriteLine(message);
                Console.ForegroundColor = previousColor;
            }
        }
    }
}