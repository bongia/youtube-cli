using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MasDev.YouTube.Model;
using MasDev.YouTube.Services;

namespace MasDev.YouTube.Download
{
    public delegate void DownloadStartHandler(YouTubeDownloadOperation sender);
    public delegate void DownloadFinishedHandler(YouTubeDownloadOperation sender);
    public delegate void DownloadServiceFailedHandler(YouTubeDownloadOperation sender, IYouTubeDownloadService service, Exception error);
    public delegate void DownloadSuccessfullyCompletedHandler(YouTubeDownloadOperation sender, string savedFile, decimal? averageSpeedInKb);
    public delegate void DownloadProgressHandler(YouTubeDownloadOperation sender, decimal? completionPercentage, decimal? speedInKb);
    public delegate void DownloadErrorHandler(YouTubeDownloadOperation sender, Exception exception);

    /// <summary>
    /// This class can be used to download a Video.
    /// The download itself is controlled by the DownloadServices specified in the options.!--
    /// If the services are designed to download audio, this class will download audio.
    /// If the services are designed to download video, this class will download video.
    /// </summary>
    public abstract class YouTubeDownloadOperation : UniqueModel
    {
        /// <summary>
        /// This event is invoked while the download starts
        /// </summary>
        public event DownloadStartHandler Start;

        /// <summary>
        /// This event is invoked after the download finishes. It is invoked regardless of the result of the download operation
        /// </summary>
        public event DownloadFinishedHandler Finish;

        /// <summary>
        /// This event is invoked after the download operation finished successfully and a file was saved to disk
        /// </summary>
        public event DownloadSuccessfullyCompletedHandler Success;

        /// <summary>
        /// This event is invoked when a frame of the file to download is streamed from the DownloadService
        /// </summary>
        public event DownloadProgressHandler ProgressChange;

        /// <summary>
        /// This event is invoked when all the DownloadServices fail in the download operation
        /// </summary>
        public event DownloadErrorHandler Error;

        /// <summary>
        /// This event invoked when a DownloadService fails in the download operation
        /// </summary>
        public event DownloadServiceFailedHandler ServiceFail;

        /// <summary>
        /// The video to be downloaded
        /// </summary>
        public readonly YouTubeVideoInfo Video;

        /// <summary>
        /// The options used to configure this download operation
        /// </summary>
        public readonly YouTubeDownloadOptions Options;

        internal readonly ITaskReference TaskReference;
        private bool _isDownloading;
        private readonly object _lock = new object();
        private readonly ISet<decimal> _speeds = new HashSet<decimal>();

        protected YouTubeDownloadOperation(YouTubeVideoInfo info, YouTubeDownloadOptions options)
        {
            Video = info;
            Options = options;
            TaskReference = new DownloaderTaskReference(this);
        }

        /// <summary>
        /// All implementations of this method shoud use the service parameter as a descriptor to configure the HttpRequest made to download the file and write it to the localPath parameter
        /// </summary>
        protected abstract Task DownloadAsync(IYouTubeDownloadService service, string localPath);

        /// <summary>
        /// Starts the DownloadOperation based on the given Options. This method can run only if there are no other download operation runnning on this instance
        /// </summary>
        public async Task DownloadAsync()
        {
            lock (_lock)
            {
                if (_isDownloading)
                    throw new NotSupportedException("Download already in progress");
                _isDownloading = true;
            }

            Start?.Invoke(this);

            try
            {
                await DownloadAsyncInternal(0);
            }
            catch (Exception e)
            {
                Error?.Invoke(this, e);
            }
            finally
            {
                Finish?.Invoke(this);
                lock (_lock)
                    _isDownloading = false;
            }
        }

        private async Task DownloadAsyncInternal(int serviceIndex)
        {
            var service = Options.Services[serviceIndex];
            try
            {
                await DownloadAsyncInternal(service);
            }
            catch (Exception e)
            {
                if (serviceIndex == Options.Services.Count - 1)
                    throw;

                ServiceFail?.Invoke(this, service, e);
                await DownloadAsyncInternal(serviceIndex + 1);
            }
        }

        private async Task DownloadAsyncInternal(IYouTubeDownloadService service)
        {
            _speeds.Clear();
            var localPath = default(string);
            try
            {
                var extension = service.Extension;
                var fileName = CleanFileName($"{Video.Title}.{extension}");
                localPath = Path.Combine(Options.DownloadFolder, fileName);

                var counter = 1;
                while (File.Exists(localPath) && Options.DownloadStrategy == YouTubeDownloadStrategy.CreateCopyIfExisting)
                    localPath = Path.Combine(Options.DownloadFolder, fileName.Replace($".{extension}", $" (Copy {counter++}).{extension}"));

                if (File.Exists(localPath) && Options.DownloadStrategy == YouTubeDownloadStrategy.DoNotDownloadIfExisting)
                    return;

                await DownloadAsync(service, localPath);

                CheckMinimumFileSizeIsConsistent(service, localPath);
                Success?.Invoke(this, localPath, _speeds.Any() ? (decimal?)_speeds.Average() : null);
                _speeds.Clear();
            }
            catch
            {
                if (localPath != null && File.Exists(localPath))
                    File.Delete(localPath);
                throw;
            }
        }

        protected virtual void CheckMinimumFileSizeIsConsistent(IYouTubeDownloadService downloadService, string localPath)
        {
            if (!(downloadService.MinimumFileSize.HasValue && File.Exists(localPath)))
                return;

            var fileInfo = new FileInfo(localPath);
            if (fileInfo.Length < downloadService.MinimumFileSize.Value)
                throw new MinimumFileSizeViolatedException(downloadService.MinimumFileSize.Value, fileInfo);
        }

        protected void PublishDownloadProgress(decimal? completionPercentage, decimal speedInKb)
        {
            _speeds.Add(speedInKb);
            ProgressChange?.Invoke(this, completionPercentage, speedInKb);
        }

        private static string CleanFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }
    }

    public class MinimumFileSizeViolatedException : Exception
    {
        public readonly long MinimumFileSize;
        public readonly FileInfo FileInfo;

        public MinimumFileSizeViolatedException(long minimumFileSize, FileInfo fileInfo) : base($"File {fileInfo.FullName} has size {fileInfo.Length} which is less than the minimum allowed ({minimumFileSize})")
        {
            MinimumFileSize = minimumFileSize;
            FileInfo = fileInfo;
        }
    }

    internal class DownloaderTaskReference : ITaskReference
    {
        public readonly YouTubeDownloadOperation Downloader;
        public DownloaderTaskReference(YouTubeDownloadOperation downloader)
        {
            Downloader = downloader;
        }

        public async Task ExecuteAsync()
        {
            await Downloader.DownloadAsync();
        }
    }
}