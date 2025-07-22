using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using Collector.Common.Extensions.Strings;
using Collector.Common.DOM;
using Collector.Common.Models.Articles;

namespace Collector.Common
{
    public static class Charlotte
    {
        public static string RouterEndpoint { get; set; }
        public static int MaxRedirects { get; set; } = 12;

        public static string Download(string url, out string newUrl)
        {
            //first, try to get headers for the URL from the host

            var method = "HEAD";
            var contentType = "";
            var status = 0;
            var wasHttp = url.IndexOf("http://") >= 0;
            var triedHttpAgain = false;
            var wwwAdded = false;
            var i = 0;

            if (wasHttp == true)
            {
                //change to https protocol
                url = url.Replace("http://", "https://");
            }

            while ((status < 301 && status > 200) || status == 0)
            {
                i++;
                status = 0;
                using (var request = new HttpClient())
                {
                    if (i > MaxRedirects) { break; }//break on too many iterations
                    try
                    {
                        //try downloading head first to see if the request is actually html or a file
                        request.Timeout = TimeSpan.FromSeconds(10);
                        try
                        {
                            using (HttpResponseMessage response = request.Send(new HttpRequestMessage()
                            {
                                Method = method == "GET" ? HttpMethod.Get : HttpMethod.Head,
                                RequestUri = new Uri(url)
                            }))
                            {
                                if (response == null && wasHttp == true && url.IndexOf("https://") == 0 && triedHttpAgain == false)
                                {
                                    //try going back to http protocol
                                    url = url.Replace("https://", "http://");
                                    method = "HEAD";
                                    triedHttpAgain = true;
                                    if (wwwAdded == true)
                                    {
                                        url = url.Replace("www.", "");
                                        wwwAdded = false;
                                    }
                                    continue;
                                }
                                if ((response == null || response.StatusCode == HttpStatusCode.MethodNotAllowed) && method == "HEAD")
                                {
                                    //try GET method instead
                                    method = "GET";
                                    continue;
                                }
                                else if (response == null && wwwAdded == false && url.IndexOf("/www.") < 0)
                                {
                                    //try adding www. to the URL
                                    wwwAdded = true;
                                    url = url.Replace("https://", "").Replace("http://", "");
                                    url = "https://www." + url;
                                    method = "HEAD";
                                    continue;
                                }
                                else if (response == null)
                                {
                                    //if all else fails, don't get response
                                    break;
                                }
                                contentType = response.Content.Headers.ContentType != null ? response.Content.Headers.ContentType.ToString().Split(";")[0] : "";
                                status = (int)response.StatusCode;

                                var header = response.Content.Headers.Where(a => a.Key == "Location").FirstOrDefault().Value;
                                var location = header != null ? CleanUrl(header.FirstOrDefault()) : "";
                                if (location != "" && location != CleanUrl(url))
                                {
                                    //url redirect (301, 302, or 303)
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
                    catch (Exception)
                    {
                        status = 500;
                    }
                    if (status != 200 && method == "HEAD")
                    {
                        //try GET method instead
                        status = 0;
                        method = "GET";
                        continue;
                    }
                    else if (status > 303 && method == "GET" && wasHttp == true && url.IndexOf("https://") >= 0)
                    {
                        //try getting request after going back to http protocol
                        url = url.Replace("https://", "http://");
                        method = "HEAD";
                        status = 0;
                        continue;
                    }
                    else if (status != 200 && url.IndexOf("/www.") < 0)
                    {
                        wwwAdded = true;
                        url = url.Replace("https://", "").Replace("http://", "");
                        url = "https://www." + url;
                        method = "HEAD";
                        status = 0;
                        continue;
                    }
                }
            }

            newUrl = url;

            if (contentType == "text/html" || contentType == "")
            {
                //new code that calls Charlotte Web Router
                var postData = new StringBuilder();
                postData.Append(String.Format("{0}={1}&", WebUtility.HtmlEncode("url"), WebUtility.HtmlEncode(url)));
                postData.Append(String.Format("{0}={1}&", WebUtility.HtmlEncode("session"), false));
                postData.Append(String.Format("{0}={1}", WebUtility.HtmlEncode("macros"), WebUtility.HtmlEncode("?")));
                StringContent postContent = new StringContent(postData.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
                HttpClient client = new HttpClient()
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };
                HttpResponseMessage message = client.PostAsync(RouterEndpoint, postContent).GetAwaiter().GetResult();
                string result = message.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return result;
            }
            else if (contentType != "")
            {
                //handle all other files
                return "file:" + contentType;
            }
            return "";
        }

        private static string CleanUrl(string url)
        {
            url = url.Split("?")[0];
            if (url[url.Length - 1] == '/')
            {
                url = url.Substring(0, url.Length - 1);
            }
            return url;
        }
    }
}
