CREATE OR REPLACE FUNCTION public."ResetAllSequences"()
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_maxId INT;
    v_sql TEXT;
    rec RECORD;
BEGIN
    FOR rec IN
        SELECT 'SequenceAnalyzerRules'::VARCHAR(128) AS seq, 'AnalyzerRules'::VARCHAR(128) AS tbl, 'ruleId'::VARCHAR(128) AS col
        UNION ALL SELECT 'SequenceArticleBugs', 'ArticleBugs', 'bugId'
        UNION ALL SELECT 'SequenceArticles', 'Articles', 'articleId'
        UNION ALL SELECT 'SequenceDomainCollectionGroups', 'DomainCollectionGroups', 'colgroupId'
        UNION ALL SELECT 'SequenceDomainCollections', 'DomainCollections', 'colId'
        UNION ALL SELECT 'SequenceDomainTypeMatches', 'DomainTypeMatches', 'matchId'
        UNION ALL SELECT 'SequenceDomains', 'Domains', 'domainId'
        UNION ALL SELECT 'SequenceDownloadQueue', 'DownloadQueue', 'qid'
        UNION ALL SELECT 'SequenceDownloadRules', 'DownloadRules', 'ruleId'
        UNION ALL SELECT 'SequenceFeedCategories', 'FeedCategories', 'categoryId'
        UNION ALL SELECT 'SequenceFeeds', 'Feeds', 'feedId'
        UNION ALL SELECT 'SequenceJournalCategories', 'JournalCategories', 'Id'
        UNION ALL SELECT 'SequenceJournalCheckListItems', 'JournalCheckListItems', 'Id'
        UNION ALL SELECT 'SequenceJournalCheckLists', 'JournalCheckLists', 'Id'
        UNION ALL SELECT 'SequenceJournalEntrySnapshots', 'JournalEntrySnapshots', 'Id'
        UNION ALL SELECT 'SequenceJournalFiles', 'JournalFiles', 'Id'
        UNION ALL SELECT 'SequenceJournalImages', 'JournalImages', 'Id'
        UNION ALL SELECT 'SequenceJournalVideos', 'JournalVideos', 'Id'
        UNION ALL SELECT 'SequenceJournals', 'Journals', 'Id'
        UNION ALL SELECT 'SequenceStatisticsProjects', 'StatisticsProjects', 'projectId'
        UNION ALL SELECT 'SequenceStatisticsResults', 'StatisticsResults', 'statId'
        UNION ALL SELECT 'SequenceSubjects', 'Subjects', 'subjectId'
        UNION ALL SELECT 'SequenceWords', 'Words', 'wordId'
    LOOP
        v_sql := format('SELECT COALESCE(MAX(%I), 0) FROM public.%I', rec.col, rec.tbl);
        EXECUTE v_sql INTO v_maxId;

        IF v_maxId > 0 THEN
            v_sql := format('ALTER SEQUENCE public.%I RESTART WITH %s', rec.seq, v_maxId + 1);
        ELSE
            v_sql := format('ALTER SEQUENCE public.%I RESTART WITH 1', rec.seq);
        END IF;

        EXECUTE v_sql;
    END LOOP;
END;
$$;