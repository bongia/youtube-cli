using System;
using System.Collections.Generic;
using System.IO;
using MasDev.YouTube.Download;
using MasDev.YouTube.Services;

namespace MasDev.YouTube.Model
{
    /// <summary>
    ///  This class is required to configure the YouTubeClient
    /// </summary>
    public class YouTubeClientOptions
    {
        public readonly string ApiKey;

        public YouTubeClientOptions(string apiKey)
        {
            ApiKey = apiKey;
        }
    }

    /// <summary>
    ///  This class is required to configure a download operation
    /// </summary>
    public class YouTubeDownloadOptions
    {
        /// <summary>
        ///  The path of the folder where to save the downloaded files
        /// </summary>
        public readonly string DownloadFolder;

        /// <summary>
        ///  Specifies how to store files in the DownloadFolder
        /// </summary>
        public YouTubeDownloadStrategy DownloadStrategy { get; set; }

        /// <summary>
        /// Specifies which services will be used to perform the download operation.
        /// This is a priority list: the first service will be used by default, if the service fails, it will be used the next service.
        /// If all the service fail, an exception is thrown
        /// </summary>
        public IReadOnlyList<IYouTubeDownloadService> Services;

        public YouTubeDownloadOptions(string downloadFolder)
        {
            DownloadFolder = downloadFolder;
        }

        internal virtual void Validate()
        {
            if (string.IsNullOrEmpty(DownloadFolder))
                throw new NotSupportedException($"invalid {nameof(DownloadFolder)} \"{DownloadFolder}\"");

            if (Services == null || Services.Count == 0)
                throw new NotSupportedException($"{nameof(Services)} must be a non empty collection");

            if (DownloadFolder != null && !Directory.Exists(DownloadFolder))
                Directory.CreateDirectory(DownloadFolder);
        }
    }

    public class YouTubeVideoCollectionDownloadOptions : YouTubeDownloadOptions
    {
        /// <summary>
        /// Specifies how many playlist items will be downloaded simultaneously
        /// </summary>
        public int ParallelismLevel { get; set; }

        /// <summary>
        /// It controls how the DownloadServices are invoked by instantiating an YouTubeDownloader when needed
        /// </summary>
        public IYouTubeDownloadOperationFactory Factory { get; set; }

        public YouTubeVideoCollectionDownloadOptions(string downloadFolder) : base(downloadFolder)
        {
            ParallelismLevel = 1;
        }

        internal override void Validate()
        {
            base.Validate();

            if (ParallelismLevel < 1)
                throw new NotSupportedException($"{nameof(ParallelismLevel)} must be > 1");

            if (Factory == null)
                throw new NotSupportedException($"{nameof(Factory)} must have a value");
        }
    }
}