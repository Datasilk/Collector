using System;

namespace Collector.Data.Entities
{
    public class SearchEngine
    {
        public int SearchEngineId { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        /// <summary>Content type this engine searches: web, image, video, news, science-journal, torrent, or any custom type discovered/added over time.</summary>
        public string Type { get; set; } = "web";
        /// <summary>HTTP method used to query the engine (GET or POST).</summary>
        public string Method { get; set; } = "GET";
        /// <summary>Optional JSON object of header name/value pairs sent with the request.</summary>
        public string HeaderVars { get; set; }
        /// <summary>Optional JSON object of POST body vars; values may contain the {search} placeholder.</summary>
        public string RequestBodyVars { get; set; }
        /// <summary>POST body encoding: form (application/x-www-form-urlencoded) or json.</summary>
        public string RequestBodyDataType { get; set; } = "form";
        /// <summary>Page URL the engine was auto-discovered from (null when manually added).</summary>
        public string DiscoveredFrom { get; set; }
        public int Sort { get; set; }
        public bool Enabled { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
