using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MasDev.YouTube.Features;
using MasDev.YouTube.Model;
using Newtonsoft.Json;

namespace MasDev.YouTube
{
    /// <summary>
    ///  Provides a base class for using YouTube features
    /// </summary>
    public class YouTubeClient : IDisposable
    {
        /// <summary>
        ///  The options used to configure this instance of the class
        /// </summary>
        public readonly YouTubeClientOptions Options;

        /// <summary>
        ///  Gives access to features related to playlist such as downloading and streaming
        /// </summary>
        public readonly YouTubePlaylistFeatures Playlists;
        private readonly HttpClient _httpClient;
        const string BaseUrl = "https://www.googleapis.com/youtube/v3/";

        public YouTubeClient(YouTubeClientOptions options)
        {
            Options = options;
            Playlists = new YouTubePlaylistFeatures(this);
            _httpClient = new HttpClient();
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        internal async Task<T> GetAsync<T>(string requestUri, IEnumerable<Param> queryParameters)
        {
            var serializer = new JsonSerializer();
            var response = await _httpClient.GetAsync(BuildRequestUri(requestUri, queryParameters));
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Http error: {response.StatusCode}");

            using (var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync()))
            using (var jsonTextReader = new JsonTextReader(streamReader))
                return serializer.Deserialize<T>(jsonTextReader);
        }

        internal async Task<dynamic> GetAsync(string requestUri, IEnumerable<Param> queryParameters)
        {
            var serializer = new JsonSerializer();
            var response = await _httpClient.GetAsync(BuildRequestUri(requestUri, queryParameters));
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Http error: {response.StatusCode}");

            using (var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync()))
            using (var jsonTextReader = new JsonTextReader(streamReader))
                return serializer.Deserialize(jsonTextReader);
        }

        private string BuildRequestUri(string requestUri, IEnumerable<Param> queryParameters)
        {
            var builder = new StringBuilder(BaseUrl);
            builder
                .Append(requestUri)
                .Append("?key=")
                .Append(Options.ApiKey);

            if (queryParameters == null)
                return builder.ToString();

            foreach (var argument in queryParameters)
                builder.Append('&')
                    .Append(WebUtility.UrlEncode(argument.Name))
                    .Append('=')
                    .Append(WebUtility.UrlEncode(argument.Value));

            return builder.ToString();
        }
    }

    internal class Param
    {
        public readonly string Name;
        public readonly string Value;

        public Param(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    internal class Params : List<Param>
    {
        public void Add(string name, string value)
        {
            Add(new Param(name, value));
        }
    }
}