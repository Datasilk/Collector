namespace Collector.Common
{
    public class Domains
    {

        public static int MinDownloadIntervals { get; set; } = 60; //in seconds

        #region "Domain Types"
        public static Dictionary<int, string> Types { get; set; } = new Dictionary<int, string>()
        {
            {0, "Unused" },
            {1, "Website" },
            {2, "E-Commerce" },
            {3, "Wiki" },
            {4, "Blog" },
            {5, "Journal" },
            {6, "SASS" },
            {7, "Social Network" },
            {8, "Advertiser" },
            {9, "Search Engine" },
            {10, "Portfolio" },
            {11, "News" },
            {12, "Travel" },
            {13, "Aggregator" },
            {14, "Government" },
            {15, "E-Books" },
            {16, "Crypto" },
            {17, "Law" },
            {18, "Medical" },
            {19, "Political" },
            {20, "Software Development" },
            {21, "Photo Gallery" },
            {22, "Videos" },
            {23, "Music" },
            {24, "Maps" },
            {25, "Mobile Apps" },
            {26, "Video Games" },
            {27, "Erotic (XXX)" },
            {28, "Conspiracy" },
            {29, "Religion" },
            {30, "Weather" },
            {31, "Comics" },
            {32, "Gore" },
            {33, "Real Estate" },
            {34, "3D Animation" },
            {35, "Live Streaming" },
            {36, "History" },
            {37, "Guns & Weapons" },
            {38, "Magazine" },
            {39, "Space" },
            {40, "Directory" },
            {41, "Propaganda" },
            {42, "Freelance" },
            {43, "Job Board" },
            {44, "Events" },
            {45, "Nonprofit" },
            {46, "Forum" },
            {47, "Dark Web" },
            {48, "Torrent" },
            {49, "Education" },
            {50, "Software" },
            {51, "Business" },
            {52, "Podcast" },
            {53, "ISP" },
            {54, "Domain Registrar" },
            {55, "Email Service" },
            {56, "Cloud Platform" },
            {57, "Storage Service" },
            {58, "Vehicles" },
            {59, "Technology" },
            {60, "Artificial Intelligence" },
            {61, "Energy" },
            {62, "Photography" },
            {63, "Archive" },
            {64, "Art" },
            {65, "Environmental" },
            {66, "Local" },
            {67, "Cultural" },
            {68, "Parked Domain" },
            {69, "Financial" },
            {70, "Tool" },
            {71, "Organization" },
            {72, "Stock Market" },
            {73, "Crowd Funding" },
            {74, "Firm" },
            {75, "Web Design" },
            {76, "Graphic Design" },
            {77, "Marketing" },
            {78, "Architecture" },
            {79, "Venture Capital" },
            {80, "Fitness" },
            {81, "Health" },
            {82, "Food" },
            {83, "Recipes" },
            {84, "Library" },
            {85, "Q&A" },
            {86, "Corporate" },
            {87, "Internet" },
            {88, "Browser" },
            {89, "Operating System (OS)" },
            {90, "Reviews" },
            {91, "How To's" },
            {92, "Institution" },
            {93, "Nature" },
            {94, "Science" },
            {95, "Agency" },
            {96, "Committee" },
            {97, "Association" },
            {98, "Sports" },
            {99, "Memorials" },
            {100, "Community" },
            {101, "Academy" },
            {102, "Comedy" },
            {103, "Chat" },
            {104, "Restaurant" },
            {105, "Delivery" },
            {106, "Analytics" },
            {107, "Camping/Hiking" },
            {108, "Equipment" },
            {109, "Council" },
            {110, "Security" },
            {111, "Parks" },
            {112, "Services" },
            {113, "Archaeology" },
            {114, "Agriculture" },
            {115, "National" },
            {116, "International" },
            {117, "Telecommunications" },
            {118, "Union" },
            {119, "Radio" },
            {120, "Club" },
            {121, "Author" },
            {122, "Scam" },
            {123, "Conference" },
            {124, "Vacation" },
            {125, "Rental" },
            {126, "Language" },
            {127, "B2B" },
            {128, "Stock" },
            {129, "Disability" },
            {130, "Television" },
            {131, "Gambling" },
            {132, "NFT" },
            {133, "Currency" },
            {134, "Platform" },
            {135, "Research" },
            {136, "Metaverse" },
            {137, "Competition" },
            {138, "Web Apps" },
            {139, "Desktop Apps" },
            {140, "Linux Apps" },
            {141, "Web Scraping" },
            {142, "Apps" },
            {143, "AI Agent" },
            {144, "DevOps" },
            {145, "Open Source" },
            {146, "App Builder" },
            {147, "Bank" },
            {148, "Hospital" },
            {149, "Police" },
            {150, "U.S. State" },
            {151, "Museum" },
            {152, "Streaming Service" }
        };

        public static KeyValuePair<int, string>[] TypesOrdered { get; set; } = Types.OrderBy(a => a.Value).ToArray();
        #endregion

        #region "Download Rules"

        public class DownloadRuleMatch
        {
            public string Url { get; set; } = "";
            public string Title { get; set; } = "";
            public string Summary { get; set; } = "";
        }

        public static DownloadRuleMatch[] DefaultDownloadLinksOnlyRules = new DownloadRuleMatch[]
        {
            new DownloadRuleMatch(){Url = "/page/"},
            new DownloadRuleMatch(){Url = "/category/"},
            new DownloadRuleMatch(){Url = "/subject/"},
            new DownloadRuleMatch(){Url = "/subjects/"},
            new DownloadRuleMatch(){Url = "/sitemap/"},
            new DownloadRuleMatch(){Url = "/tag/"},
            new DownloadRuleMatch(){Url = "/tags"},
            new DownloadRuleMatch(){Url = "/genre/"},
            new DownloadRuleMatch(){Url = "/genres/"}
        };

        //set of default download rules for URLs that will not be downloaded or used as articles
        public static DownloadRuleMatch[] DefaultDoNotDownloadRules = new DownloadRuleMatch[]
        {
            new DownloadRuleMatch(){Url = "/sponsor"},
            new DownloadRuleMatch(){Url = "/metrics"},
            new DownloadRuleMatch(){Url = ".jpg"},
            new DownloadRuleMatch(){Url = "/cart"},
            new DownloadRuleMatch(){Url = "/order/"},
            new DownloadRuleMatch(){Url = "/shop/"},
            new DownloadRuleMatch(){Title = "page not found"},
            new DownloadRuleMatch(){Title = "file not found"},
            new DownloadRuleMatch(){Title = "sign in"},
            new DownloadRuleMatch(){Title = "log in"},
            new DownloadRuleMatch(){Title = "access denied"},
            new DownloadRuleMatch(){Title = "404"},
        };

        public static bool CheckDefaultDownloadLinksOnlyRules(string url, string title, string summary)
        {
            foreach (var rule in DefaultDownloadLinksOnlyRules)
            {
                if (CheckDownloadRule(rule.Url, rule.Title, rule.Summary, url, title, summary) == true)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool CheckDefaultDoNotDownloadRules(string url, string title, string summary)
        {
            foreach (var rule in DefaultDoNotDownloadRules)
            {
                if (CheckDownloadRule(rule.Url, rule.Title, rule.Summary, url, title, summary) == true)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool CheckDownloadRule(string urlMatch, string titleMatch, string summaryMatch, string url, string title, string summary)
        {
            if (urlMatch != "" && url != "")
            {
                urlMatch = urlMatch.ToLower();
                url = url.ToLower();
            }
            if (titleMatch != "" && title != "")
            {
                titleMatch = titleMatch.ToLower();
                title = title.ToLower();
            }
            if (summaryMatch != "" && summary != "")
            {
                summaryMatch = summaryMatch.ToLower();
                summary = summary.ToLower();
            }
            if ((urlMatch != "" && url != "" && url.IndexOf(urlMatch) > -1) ||
                (titleMatch != "" && title != "" && title.IndexOf(titleMatch) > -1) ||
                (summaryMatch != "" && summary != "" && summary.IndexOf(summaryMatch) > -1))
            {
                return true;
            }
            return false;
        }
        #endregion

        #region "Validation"
        public static bool ValidateURL(string url)
        {
            bool isValid = url.IndexOf("http://") == 0 || url.IndexOf("https://") == 0;
            if (isValid)
            {
                url = url.Replace("http://", "").Replace("https://", "").Replace("www.", "");
                var parts = url.Split('/');
                if (parts.Length > 0)
                {
                    parts = parts[0].Split(".");
                    if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool ValidateDomain(string domain)
        {
            //check blacklist wildcards
            foreach (var wildcard in Blacklist.Wildcards)
            {
                if (wildcard.Match(domain).Success)
                {
                    return false;
                }
            }
            var parts = domain.Split(".");
            if (parts.Length < 2) { return false; }
            if (int.TryParse(parts[parts.Length - 1], out int num))
            {
                //found potential IP address
                return false;
            }
            return true;
        }
        #endregion
    }
}
