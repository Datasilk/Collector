using System.Text.RegularExpressions;

namespace Collector.Common
{
    public static class Blacklist
    {
        public static List<Regex> Wildcards { get; set; } = new List<Regex>();
    }
}
