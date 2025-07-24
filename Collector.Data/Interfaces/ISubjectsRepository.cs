using System.Collections.Generic;
using Collector.Data.Entities;

namespace Collector.Data.Interfaces
{
    public interface ISubjectsRepository
    {
        int CreateSubject(int parentId, int grammartype, int score, string title, string breadcrumb);
        Subject GetSubjectById(int subjectId);
        Subject GetSubjectByTitle(string title, string breadcrumb);
        void Move(int subjectId, int newParentId);
        List<Subject> GetList(string subjectIds, int parentId = -1);
        List<Subject> GetByParentId(int parentId);
    }
}
