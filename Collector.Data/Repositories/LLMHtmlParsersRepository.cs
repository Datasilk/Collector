using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Dapper;

namespace Collector.Data.Repositories
{
    public class LLMHtmlParsersRepository : ILLMHtmlParsersRepository
    {
        private readonly IDbConnection _dbConnection;

        public LLMHtmlParsersRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public string Add(LLMHtmlParser model)
        {
            _dbConnection.Execute(@"
                INSERT INTO public.""LLM_HtmlParsers"" 
                (""Id"", ""Name"", ""Notes"", ""Status"") 
                VALUES (@Id, @Name, @Notes, @Status)", 
                model);
            return model.Id;
        }

        public LLMHtmlParser GetById(string id)
        {
            return _dbConnection.QuerySingleOrDefault<LLMHtmlParser>(@"
                SELECT * FROM public.""LLM_HtmlParsers"" 
                WHERE ""Id"" = @id", 
                new { id });
        }

        public LLMHtmlParser GetActive()
        {
            return _dbConnection.QuerySingleOrDefault<LLMHtmlParser>(@"
                SELECT * FROM public.""LLM_HtmlParsers"" 
                WHERE ""Status"" = 1 
                LIMIT 1");
        }

        public List<LLMHtmlParser> GetAll()
        {
            return _dbConnection.Query<LLMHtmlParser>(@"
                SELECT * FROM public.""LLM_HtmlParsers"" 
                ORDER BY ""Name""").ToList();
        }

        public void Update(LLMHtmlParser model)
        {
            _dbConnection.Execute(@"
                UPDATE public.""LLM_HtmlParsers"" 
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
                UPDATE public.""LLM_HtmlParsers"" 
                SET ""Status"" = 0");

            // Activate the selected model
            _dbConnection.Execute(@"
                UPDATE public.""LLM_HtmlParsers"" 
                SET ""Status"" = 1, 
                    ""Modified"" = NOW() AT TIME ZONE 'UTC'
                WHERE ""Id"" = @id", 
                new { id });
        }

        public void Delete(string id)
        {
            _dbConnection.Execute(@"
                DELETE FROM public.""LLM_HtmlParsers"" 
                WHERE ""Id"" = @id", 
                new { id });
        }
    }
}
