namespace Collector.Data.Entities
{
    public class Word
    {
        public int wordId { get; set; }
        public string word { get; set; }
        public string subjects { get; set; }
        public int? grammartype { get; set; }
        public int? score { get; set; }
    }
}
