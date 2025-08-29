using System.Net;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Collector.Common;
using Collector.Common.Extensions.Strings;
using Collector.Common.Enums;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Collector.Data.Enums;
using Collector.Common.Models.Articles;

namespace Collector.CyberScout
{
    public static class App
    {
        public static ILogger<Program> Logger;
        public static IDownloadsRepository DownloadsRepository;
        public static IDomainsRepository DomainsRepository;
        public static IBlacklistsRepository BlacklistsRepository;
        public static IArticlesRepository ArticlesRepository;
        public static IFeedsRepository FeedsRepository;

        public static bool StopRequested = false;
        public static Dictionary<string, List<KeyValuePair<string, string>>> UrlCache = new Dictionary<string, List<KeyValuePair<string, string>>>();
        public static int DownloadsArchived = 0;

        public static bool ValidateURL(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;

            try
            {
                // Basic URL validation
                var uri = new Uri(url);
                return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
            }
            catch
            {
                return false;
            }
        }

        public static bool ValidateDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain)) return false;

            // Basic domain validation - can be expanded as needed
            return domain.Contains(".") && !domain.Contains(" ");
        }

        public static void ValidateURLs(string domain, List<DownloadRule> downloadRules, out List<string> urlsChecked)
        {
            urlsChecked = new List<string>();

            if (!App.UrlCache.ContainsKey(domain) || App.UrlCache[domain] == null)
            {
                return;
            }

            foreach (var urlPair in App.UrlCache[domain])
            {
                string url = urlPair.Key + urlPair.Value;
                bool shouldAdd = true;

                if(downloadRules != null)
                {
                    // Check against download rules
                    foreach (var rule in downloadRules)
                    {
                        if (!rule.rule && CheckDownloadRule(rule.url, rule.title, rule.summary, url, "", ""))
                        {
                            shouldAdd = false;
                            break;
                        }
                    }
                }

                if (shouldAdd)
                {
                    urlsChecked.Add(url);
                }
            }
        }

        public static bool CheckDownloadRule(string ruleUrl, string ruleTitle, string ruleSummary, string url, string title, string summary)
        {
            // Simple implementation - can be expanded as needed
            if (!string.IsNullOrEmpty(ruleUrl) && url.Contains(ruleUrl))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(ruleTitle) && !string.IsNullOrEmpty(title) && title.Contains(ruleTitle))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(ruleSummary) && !string.IsNullOrEmpty(summary) && summary.Contains(ruleSummary))
            {
                return true;
            }

            return false;
        }
    }
}
