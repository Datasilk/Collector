using System.Text.Json;

namespace Collector.API.Models
{
    public class UpdateEntryListSettingsModel
    {
        public int JournalId { get; set; }
        public JsonElement EntryList { get; set; }
    }
}
