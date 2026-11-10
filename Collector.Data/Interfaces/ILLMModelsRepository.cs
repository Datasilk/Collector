using System.Collections.Generic;
using Collector.Data.Entities;

namespace Collector.Data.Interfaces
{
    public interface ILLMModelsRepository
    {
        int Add(LLMModel model);
        LLMModel GetById(int modelId);
        List<LLMModel> GetAll();
        void Update(LLMModel model);
        void Delete(int modelId);
        void SetEnabled(int modelId, bool enabled);
        void SetPreferred(int modelId, int type);
    }
}
