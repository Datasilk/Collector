using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Dapper;

namespace Collector.Data.Repositories
{
    public class LLMModelsRepository : ILLMModelsRepository
    {
        private readonly IDbConnection _dbConnection;

        public LLMModelsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public int Add(LLMModel model)
        {
            return _dbConnection.ExecuteScalar<int>(@"
                INSERT INTO public.""LLM_Models"" 
                (""name"", ""model"", ""endpoint"", ""privateKey"", ""type"", ""enabled"", ""preferred"", ""extraBody"", ""dateupdated"") 
                VALUES (@Name, @Model, @Endpoint, @PrivateKey, @Type, @Enabled, @Preferred, @ExtraBody, CURRENT_TIMESTAMP)
                RETURNING ""modelId""", 
                model);
        }

        public LLMModel GetById(int modelId)
        {
            return _dbConnection.QuerySingleOrDefault<LLMModel>(@"
                SELECT * FROM public.""LLM_Models"" 
                WHERE ""modelId"" = @modelId", 
                new { modelId });
        }

        public List<LLMModel> GetAll()
        {
            return _dbConnection.Query<LLMModel>(@"
                SELECT * FROM public.""LLM_Models"" 
                ORDER BY ""name""").ToList();
        }

        public void Update(LLMModel model)
        {
            _dbConnection.Execute(@"
                UPDATE public.""LLM_Models"" 
                SET ""name"" = @Name, 
                    ""model"" = @Model, 
                    ""endpoint"" = @Endpoint, 
                    ""privateKey"" = @PrivateKey, 
                    ""type"" = @Type, 
                    ""enabled"" = @Enabled, 
                    ""preferred"" = @Preferred, 
                    ""extraBody"" = @ExtraBody, 
                    ""dateupdated"" = CURRENT_TIMESTAMP
                WHERE ""modelId"" = @ModelId", 
                model);
        }

        public void Delete(int modelId)
        {
            _dbConnection.Execute(@"
                DELETE FROM public.""LLM_Models"" 
                WHERE ""modelId"" = @modelId", 
                new { modelId });
        }

        public void SetEnabled(int modelId, bool enabled)
        {
            _dbConnection.Execute(@"
                UPDATE public.""LLM_Models"" 
                SET ""enabled"" = @enabled, 
                    ""dateupdated"" = CURRENT_TIMESTAMP
                WHERE ""modelId"" = @modelId", 
                new { modelId, enabled });
        }

        public void SetPreferred(int modelId, int type)
        {
            // Only one model of each type can be preferred
            _dbConnection.Execute(@"
                UPDATE public.""LLM_Models"" 
                SET ""preferred"" = FALSE,
                    ""dateupdated"" = CURRENT_TIMESTAMP
                WHERE ""type"" = @type",
                new { type });

            _dbConnection.Execute(@"
                UPDATE public.""LLM_Models"" 
                SET ""preferred"" = TRUE,
                    ""dateupdated"" = CURRENT_TIMESTAMP
                WHERE ""modelId"" = @modelId",
                new { modelId });
        }
    }
}
