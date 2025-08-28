using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Collector.Common.Extensions.Strings;

namespace Collector.Common
{
    public static class Article
    {
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        static Article()
        {
            // Set up default headers
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        }

        /// <summary>
        /// Downloads content from a URL with proper handling of redirects and content types
        /// </summary>
        /// <param name="url">The URL to download content from</param>
        /// <param name="newUrl">Output parameter that will contain any redirected URL</param>
        /// <returns>The downloaded content as a string</returns>
        public static string Download(string url, out string newUrl)
        {
            newUrl = "";
            if (string.IsNullOrEmpty(url)) return "";

            // Clean the URL
            url = url.CleanUrl();
            
            try
            {
                // First make a HEAD request to check content type
                using (var headRequest = new HttpRequestMessage(HttpMethod.Head, url))
                {
                    var headResponse = _httpClient.SendAsync(headRequest).GetAwaiter().GetResult();
                    headResponse.EnsureSuccessStatusCode();
                    
                    // Check if URL was redirected
                    if (headResponse.RequestMessage.RequestUri.ToString() != url)
                    {
                        newUrl = headResponse.RequestMessage.RequestUri.ToString();
                    }
                    
                    // Check content type
                    string contentType = headResponse.Content.Headers.ContentType?.MediaType?.ToLower() ?? "";
                    if (!contentType.Contains("text/html") && 
                        !contentType.Contains("application/xhtml") && 
                        !contentType.Contains("text/plain") &&
                        !contentType.Contains("application/json") &&
                        !contentType.Contains("application/xml"))
                    {
                        // Return file type for non-HTML content
                        return "file:" + contentType;
                    }
                }
                
                // Now make a GET request to download the content
                string requestUrl = string.IsNullOrEmpty(newUrl) ? url : newUrl;
                var getResponse = _httpClient.GetAsync(requestUrl).GetAwaiter().GetResult();
                getResponse.EnsureSuccessStatusCode();
                
                // Check again for redirects
                if (getResponse.RequestMessage.RequestUri.ToString() != url && string.IsNullOrEmpty(newUrl))
                {
                    newUrl = getResponse.RequestMessage.RequestUri.ToString();
                }
                
                return getResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return "Error: Page not found (404)";
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                return "Error: Access forbidden (403)";
            }
            catch (TaskCanceledException)
            {
                return "Error: Request timed out";
            }
            catch (HttpRequestException)
            {
                throw; // Re-throw other HTTP exceptions
            }
            catch (Exception)
            {
                throw; // Re-throw general exceptions
            }
        }
    }
}
