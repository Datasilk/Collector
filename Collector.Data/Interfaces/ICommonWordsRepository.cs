namespace Collector.Data.Interfaces
{
    public interface ICommonWordsRepository
    {
        void Add(string[] words);
        List<string> GetList();
    }
}
