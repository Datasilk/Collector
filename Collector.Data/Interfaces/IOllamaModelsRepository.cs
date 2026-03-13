using System.Collections.Generic;
using Collector.Data.Entities;

namespace Collector.Data.Interfaces
{
    public interface IOllamaModelsRepository
    {
        string Add(OllamaModel model);
        OllamaModel GetById(string id);
        OllamaModel GetActive();
        List<OllamaModel> GetAll();
        void Update(OllamaModel model);
        void SetActive(string id);
        void Delete(string id);
    }
}
