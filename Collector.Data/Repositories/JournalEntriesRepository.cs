using Dapper;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class JournalEntriesRepository : IJournalEntriesRepository
    {
        readonly IDbConnection _dbConnection;

        public JournalEntriesRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public Guid Add(JournalEntry journalEntry)
        {
            journalEntry.Id = Guid.NewGuid();
            _dbConnection.Execute(@"INSERT INTO [dbo].[JournalEntries] 
                ([Id], [JournalId], [ParentEntryId], [Title], [Description], [Url]) 
                VALUES (@id, @journalId, @parentEntryId, @title, @description, @url)", 
                new { 
                    id = journalEntry.Id,
                    journalId = journalEntry.JournalId, 
                    parentEntryId = journalEntry.ParentEntryId,
                    title = journalEntry.Title, 
                    description = journalEntry.Description,
                    url = journalEntry.Url
                });
            return journalEntry.Id;
        }

        public void Rename(Guid journalEntryId, string title)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Title] = @title 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId, title });
        }

        public void UpdateDescription(Guid journalEntryId, string description)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Description] = @description 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId, description });
        }

        public void Archive(Guid journalEntryId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Status] = 0 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId });
        }

        public void Unarchive(Guid journalEntryId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Status] = 1 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId });
        }

        public void Publish(Guid journalEntryId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Status] = 2 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId });
        }

        public void Modify(Guid journalEntryId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Modified] = GETUTCDATE() 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId });
        }

        public List<JournalEntry> GetAllByJournalId(int journalId)
        {
            return _dbConnection.Query<JournalEntry>(@"SELECT je.*, parent.[Title] AS ParentEntryName
                FROM [dbo].[JournalEntries] je
                LEFT JOIN [dbo].[JournalEntries] parent ON je.[ParentEntryId] = parent.[Id]
                WHERE je.[JournalId] = @journalId
                AND je.[Status] > 0
                ORDER BY je.[Created] DESC", 
                new { journalId }).ToList();
        }

        public JournalEntryFilterResult Filter(int journalId, string search, string sort, int start, int length, List<int> tagIds)
        {
            // Normalize inputs
            search = search ?? string.Empty;
            sort = string.IsNullOrWhiteSpace(sort) ? "Created_desc" : sort;

            // Build ORDER BY clause based on sort string
            string orderBy;
            switch (sort)
            {
                case "Title_asc":
                    orderBy = "[Title] ASC";
                    break;
                case "Title_desc":
                    orderBy = "[Title] DESC";
                    break;
                case "Created_asc":
                    orderBy = "[Created] ASC";
                    break;
                case "Created_desc":
                    orderBy = "[Created] DESC";
                    break;
                case "Modified_asc":
                    orderBy = "[Modified] ASC";
                    break;
                case "Modified_desc":
                    orderBy = "[Modified] DESC";
                    break;
                case "Status_asc":
                    orderBy = "[Status] ASC";
                    break;
                case "Status_desc":
                    orderBy = "[Status] DESC";
                    break;
                default:
                    orderBy = "[Created] DESC";
                    break;
            }

            var baseWhere = "je.[JournalId] = @journalId AND je.[Status] > 0";
            if (!string.IsNullOrWhiteSpace(search))
            {
                baseWhere += " AND (je.[Title] LIKE @search OR je.[Description] LIKE @search)";
            }

            var hasTags = tagIds != null && tagIds.Count > 0;
            List<int> distinctTagIds = null;
            int tagCount = 0;
            if (hasTags)
            {
                distinctTagIds = tagIds.Distinct().ToList();
                tagCount = distinctTagIds.Count;
            }

            string countSql;
            string dataSql;

            if (hasTags)
            {
                // Filter based on JournalEntryTags join, enforcing that entries contain ALL provided tags
                var tagFilteredSubquery = $@"
                    SELECT je.[Id]
                    FROM [dbo].[JournalEntryTags] jet
                    INNER JOIN [dbo].[JournalEntries] je ON je.[Id] = jet.[JournalEntryId]
                    WHERE {baseWhere}
                      AND jet.[TagId] IN @tagIds
                    GROUP BY je.[Id]
                    HAVING COUNT(DISTINCT jet.[TagId]) = @tagCount";

                countSql = $"SELECT COUNT(*) FROM ({tagFilteredSubquery}) AS matches";

                dataSql = $@"
                    SELECT je.*, parent.[Title] AS ParentEntryName
                    FROM [dbo].[JournalEntryTags] jet
                    INNER JOIN [dbo].[JournalEntries] je ON je.[Id] = jet.[JournalEntryId]
                    LEFT JOIN [dbo].[JournalEntries] parent ON je.[ParentEntryId] = parent.[Id]
                    WHERE {baseWhere}
                      AND jet.[TagId] IN @tagIds
                    GROUP BY je.[Id], je.[JournalId], je.[ParentEntryId], je.[Title], je.[Description], je.[Created], je.[Modified], je.[Status], je.[ChapterId], je.[Encrypted], je.[Thumbnail], je.[Url], parent.[Title]
                    HAVING COUNT(DISTINCT jet.[TagId]) = @tagCount
                    ORDER BY {orderBy}
                    OFFSET @start ROWS FETCH NEXT @length ROWS ONLY";
            }
            else
            {
                // Simpler path when no tags are provided: query directly from JournalEntries
                countSql = $"SELECT COUNT(*) FROM [dbo].[JournalEntries] je WHERE {baseWhere}";

                dataSql = $@"
                    SELECT je.*, parent.[Title] AS ParentEntryName
                    FROM [dbo].[JournalEntries] je
                    LEFT JOIN [dbo].[JournalEntries] parent ON je.[ParentEntryId] = parent.[Id]
                    WHERE {baseWhere}
                    ORDER BY {orderBy}
                    OFFSET @start ROWS FETCH NEXT @length ROWS ONLY";
            }

            var parameters = new
            {
                journalId,
                search = string.IsNullOrWhiteSpace(search) ? null : $"%{search}%",
                start,
                length,
                tagIds = hasTags ? distinctTagIds : null,
                tagCount
            };

            var totalCount = _dbConnection.ExecuteScalar<int>(countSql, parameters);
            var entries = _dbConnection.Query<JournalEntry>(dataSql, parameters).ToList();

            return new JournalEntryFilterResult
            {
                Entries = entries,
                TotalCount = totalCount
            };
        }

        public JournalEntry GetById(Guid journalEntryId)
        {
            return _dbConnection.QuerySingleOrDefault<JournalEntry>(@"SELECT je.*, parent.[Title] AS ParentEntryName
                FROM [dbo].[JournalEntries] je
                LEFT JOIN [dbo].[JournalEntries] parent ON je.[ParentEntryId] = parent.[Id]
                WHERE je.[Id] = @journalEntryId", 
                new { journalEntryId });
        }

        public void UpdateJournalId(Guid journalEntryId, int journalId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [JournalId] = @journalId 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId, journalId });
        }

        public void UpdateLastModified(Guid journalEntryId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Modified] = GETUTCDATE() 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId });
        }

        public void UpdateCreated(Guid journalEntryId, DateTime created)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Created] = @created 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId, created });
        }

        public void SetEncrypted(Guid journalEntryId, bool encrypted)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Encrypted] = @encrypted
                WHERE [Id] = @journalEntryId",
                new { journalEntryId, encrypted });
        }

        public void SetPublished(Guid journalEntryId, bool isPublished)
        {
            var status = isPublished ? 2 : 1; // 2 = Published, 1 = Active
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Status] = @status
                WHERE [Id] = @journalEntryId",
                new { journalEntryId, status });
        }

        public void SetChapter(Guid journalEntryId, int? chapterId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [ChapterId] = @chapterId
                WHERE [Id] = @journalEntryId",
                new { journalEntryId, chapterId });
        }

        public void UpdateThumbnail(Guid journalEntryId, string thumbnail, string thumbnailModuleId = null)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Thumbnail] = @thumbnail, [ThumbnailModuleId] = @thumbnailModuleId
                WHERE [Id] = @journalEntryId",
                new { journalEntryId, thumbnail, thumbnailModuleId });
        }

        public void SetParent(Guid journalEntryId, Guid? parentEntryId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries]
                SET [ParentEntryId] = @parentEntryId
                WHERE [Id] = @journalEntryId",
                new { journalEntryId, parentEntryId });
        }
    }
}
