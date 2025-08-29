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
        public static string BrowserEndpoint { get; set; } = "http://localhost:7007/GetDOM";

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
            
            var method = "HEAD";
            var contentType = "";
            var status = 0;
            var wasHttp = url.IndexOf("http://") >= 0;
            var triedHttpAgain = false;
            var wwwAdded = false;
            var i = 0;
            
            // Default to HTTPS if using HTTP
            if (wasHttp)
            {
                url = url.Replace("http://", "https://");
            }
            
            // Main loop for handling redirects and retries
            while ((status < 301 && status > 200) || status == 0)
            {
                i++;
                status = 0;
                
                if (i > 12) { break; } // Break on too many iterations
                
                try
                {
                    using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                    {
                        // Set up headers to mimic browser
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");
                        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                        client.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"139\", \"Google Chrome\";v=\"139\"");
                        client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
                        client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
                        client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                        client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
                        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
                        client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
                        client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                        client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
                        
                        // Try downloading head first to see if the request is actually html or a file
                        try
                        {
                            using (HttpResponseMessage response = client.SendAsync(new HttpRequestMessage()
                            {
                                Method = method == "GET" ? HttpMethod.Get : HttpMethod.Head,
                                RequestUri = new Uri(url)
                            }).GetAwaiter().GetResult())
                            {
                                if (response == null && wasHttp && url.IndexOf("https://") == 0 && !triedHttpAgain)
                                {
                                    // Try going back to HTTP protocol
                                    url = url.Replace("https://", "http://");
                                    method = "HEAD";
                                    triedHttpAgain = true;
                                    if (wwwAdded)
                                    {
                                        url = url.Replace("www.", "");
                                        wwwAdded = false;
                                    }
                                    continue;
                                }
                                if ((response == null || response.StatusCode == HttpStatusCode.MethodNotAllowed) && method == "HEAD")
                                {
                                    // Try GET method instead
                                    method = "GET";
                                    continue;
                                }
                                else if (response == null && wwwAdded == false && url.IndexOf("/www.") < 0)
                                {
                                    // Try adding www. to the URL
                                    wwwAdded = true;
                                    url = url.Replace("https://", "").Replace("http://", "");
                                    url = "https://www." + url;
                                    method = "HEAD";
                                    continue;
                                }
                                else if (response == null)
                                {
                                    // If all else fails, don't get response
                                    break;
                                }
                                
                                contentType = response.Content.Headers.ContentType != null ? 
                                    response.Content.Headers.ContentType.ToString().Split(";")[0] : "";
                                status = (int)response.StatusCode;

                                // Check for redirects - checking both headers and content headers
                                string location = "";
                                
                                // First check response headers (standard location)
                                if (response.Headers.Location != null)
                                {
                                    location = response.Headers.Location.ToString().CleanUrl();
                                }
                                // Then check content headers (as in original code)
                                else
                                {
                                    var header = response.Content.Headers.Where(a => a.Key == "Location").FirstOrDefault().Value;
                                    if (header != null)
                                    {
                                        location = header.FirstOrDefault().CleanUrl();
                                    }
                                }
                                
                                if (!string.IsNullOrEmpty(location) && location != url.CleanUrl())
                                {
                                    // URL redirect (301, 302, or 303)
                                    url = location;
                                    method = "HEAD";
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.IndexOf("No such host is known") >= 0)
                            {
                                newUrl = "";
                                return "";
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    status = 500;
                }
                
                if (status != 200 && method == "HEAD")
                {
                    // Try GET method instead
                    status = 0;
                    method = "GET";
                    continue;
                }
                else if (status > 303 && method == "GET" && wasHttp && url.IndexOf("https://") >= 0)
                {
                    // Try getting request after going back to HTTP protocol
                    url = url.Replace("https://", "http://");
                    method = "HEAD";
                    status = 0;
                    continue;
                }
                else if (status != 200 && url.IndexOf("/www.") < 0)
                {
                    // Try adding www. to the URL
                    wwwAdded = true;
                    url = url.Replace("https://", "").Replace("http://", "");
                    url = "https://www." + url;
                    method = "HEAD";
                    status = 0;
                    continue;
                }
            }
            
            // Store the final URL
            newUrl = url;
            
            // Process based on content type
            if (contentType == "text/html" || contentType == "")
            {
                // Call Charlotte Web Router
                var postData = new StringBuilder();
                postData.Append(String.Format("{0}={1}&", WebUtility.HtmlEncode("url"), WebUtility.HtmlEncode(url)));
                postData.Append(String.Format("{0}={1}&", WebUtility.HtmlEncode("session"), false));
                postData.Append(String.Format("{0}={1}", WebUtility.HtmlEncode("macros"), WebUtility.HtmlEncode("?")));
                
                StringContent postContent = new StringContent(postData.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
                
                using var client = new HttpClient()
                {
                    Timeout = TimeSpan.FromSeconds(35) // Increased timeout to accommodate Router's 30-second timeout
                };
                
                try
                {
                    HttpResponseMessage message = client.PostAsync(BrowserEndpoint, postContent).GetAwaiter().GetResult();
                    string result = message.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return result;
                }
                catch (Exception ex)
                {
                    return $"Error calling Charlotte Web Router: {ex.Message}";
                }
            }
            else if (contentType != "")
            {
                // Handle all other files
                return "file:" + contentType;
            }
            
            return "";
        }
    }
}
