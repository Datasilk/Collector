using System.Collections.Generic;
using System.Data;
using System.Linq;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Dapper;

namespace Collector.Data.Repositories
{
    public class SubjectsRepository : ISubjectsRepository
    {
        private readonly IDbConnection _dbConnection;

        public SubjectsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public int CreateSubject(int parentId, int grammartype, int score, string title, string breadcrumb)
        {
            return _dbConnection.ExecuteScalar<int>("SELECT public.\"Subject_Create\"(@parentId, @grammartype, @score, @title, @breadcrumb)", 
                new { parentId, grammartype, score, title, breadcrumb });
        }

        public Subject GetSubjectById(int subjectId)
        {
            return _dbConnection.QueryFirstOrDefault<Subject>("SELECT * FROM public.\"Subject_GetById\"(@subjectId)", new { subjectId });
        }

        public Subject GetSubjectByTitle(string title, string breadcrumb)
        {
            return _dbConnection.QueryFirstOrDefault<Subject>("SELECT * FROM public.\"Subject_GetByTitle\"(@title, @breadcrumb)", new { title, breadcrumb });
        }

        public void Move(int subjectId, int newParentId)
        {
            _dbConnection.Execute("SELECT public.\"Subject_Move\"(@subjectId, @newParentId)", new { subjectId, newParentId });
        }

        public List<Subject> GetList(string subjectIds, int parentId = -1)
        {
            return _dbConnection.Query<Subject>("SELECT * FROM public.\"Subjects_GetList\"(@subjectIds, @parentId)", new { subjectIds, parentId }).ToList();
        }

        public List<Subject> GetByParentId(int parentId)
        {
            return _dbConnection.Query<Subject>("SELECT * FROM public.\"Subjects_GetList\"(@subjectIds, @parentId)", new { subjectIds = "", parentId }).ToList();
        }
    }
}
