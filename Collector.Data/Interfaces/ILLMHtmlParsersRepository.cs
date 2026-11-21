using System.Collections.Generic;
using Collector.Data.Entities;

namespace Collector.Data.Interfaces
{
    public interface ILLMHtmlParsersRepository
    {
        string Add(LLMHtmlParser model);
        LLMHtmlParser GetById(string id);
        LLMHtmlParser GetActive();
        List<LLMHtmlParser> GetAll();
        void Update(LLMHtmlParser model);
        void SetActive(string id);
        void Delete(string id);
    }
}
