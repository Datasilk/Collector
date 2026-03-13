using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Dapper;

namespace Collector.Data.Repositories
{
    public class OllamaModelsRepository : IOllamaModelsRepository
    {
        private readonly IDbConnection _dbConnection;

        public OllamaModelsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public string Add(OllamaModel model)
        {
            _dbConnection.Execute(@"
                INSERT INTO public.""OllamaModels"" 
                (""Id"", ""Name"", ""Notes"", ""Status"") 
                VALUES (@Id, @Name, @Notes, @Status)", 
                model);
            return model.Id;
        }

        public OllamaModel GetById(string id)
        {
            return _dbConnection.QuerySingleOrDefault<OllamaModel>(@"
                SELECT * FROM public.""OllamaModels"" 
                WHERE ""Id"" = @id", 
                new { id });
        }

        public OllamaModel GetActive()
        {
            return _dbConnection.QuerySingleOrDefault<OllamaModel>(@"
                SELECT * FROM public.""OllamaModels"" 
                WHERE ""Status"" = 1 
                LIMIT 1");
        }

        public List<OllamaModel> GetAll()
        {
            return _dbConnection.Query<OllamaModel>(@"
                SELECT * FROM public.""OllamaModels"" 
                ORDER BY ""Name""").ToList();
        }

        public void Update(OllamaModel model)
        {
            _dbConnection.Execute(@"
                UPDATE public.""OllamaModels"" 
                SET ""Name"" = @Name, 
                    ""Notes"" = @Notes, 
                    ""Modified"" = NOW() AT TIME ZONE 'UTC'
                WHERE ""Id"" = @Id", 
                model);
        }

        public void SetActive(string id)
        {
            // Deactivate all models
            _dbConnection.Execute(@"
                UPDATE public.""OllamaModels"" 
                SET ""Status"" = 0");

            // Activate the selected model
            _dbConnection.Execute(@"
                UPDATE public.""OllamaModels"" 
                SET ""Status"" = 1, 
                    ""Modified"" = NOW() AT TIME ZONE 'UTC'
                WHERE ""Id"" = @id", 
                new { id });
        }

        public void Delete(string id)
        {
            _dbConnection.Execute(@"
                DELETE FROM public.""OllamaModels"" 
                WHERE ""Id"" = @id", 
                new { id });
        }
    }
}
