using System.IO.Compression;
using HtmlAgilityPack;

namespace Moss.Aviation.Charts.Faa
{
    public static class HttpClientExtensions
    {
        public static async Task<HtmlDocument> GetHtmlDocument(this HttpClient httpClient, Uri uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var doc = new HtmlDocument();

            if (response.Content.Headers.ContentEncoding.Contains("gzip", StringComparer.InvariantCultureIgnoreCase))
            {
                using (var inputStream = await response.Content.ReadAsStreamAsync())
                using (var outputStream = new MemoryStream())
                using (var compressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    compressionStream.CopyTo(outputStream);

                    inputStream.Flush();

                    outputStream.Seek(0, SeekOrigin.Begin);

                    doc.Load(outputStream);
                }
            }
            else
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    doc.Load(stream);
                }
            }

            return doc;
        }
    }
}
