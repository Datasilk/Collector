CREATE OR REPLACE FUNCTION public."Domain_CleanDownloads"
(
    p_domainId INT
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    DROP TABLE IF EXISTS tmp_articles_to_clean;
    CREATE TEMP TABLE tmp_articles_to_clean AS
    SELECT DISTINCT a."articleId"
    FROM public."Articles" a
    JOIN public."DownloadRules" r ON r."domainId" = a."domainId"
    WHERE a."domainId" = p_domainId
    AND (
        (LENGTH(r."url") > 0 AND a."url" ILIKE '%' || r."url" || '%')
        OR (LENGTH(r."title") > 0 AND a."title" ILIKE '%' || r."title" || '%')
        OR (LENGTH(r."summary") > 0 AND a."summary" ILIKE '%' || r."summary" || '%')
    );

    DELETE FROM public."Articles" WHERE "articleId" IN (SELECT "articleId" FROM tmp_articles_to_clean);
    DELETE FROM public."ArticleBugs" WHERE "articleId" IN (SELECT "articleId" FROM tmp_articles_to_clean);
    DELETE FROM public."ArticleDates" WHERE "articleId" IN (SELECT "articleId" FROM tmp_articles_to_clean);
    DELETE FROM public."ArticleSentences" WHERE "articleId" IN (SELECT "articleId" FROM tmp_articles_to_clean);
    DELETE FROM public."ArticleSubjects" WHERE "articleId" IN (SELECT "articleId" FROM tmp_articles_to_clean);
    DELETE FROM public."ArticleWords" WHERE "articleId" IN (SELECT "articleId" FROM tmp_articles_to_clean);

    DELETE FROM public."DownloadQueue" WHERE "qid" IN (
        SELECT DISTINCT dq."qid"
        FROM public."DownloadQueue" dq
        JOIN public."DownloadRules" r ON r."domainId" = dq."domainId"
        WHERE dq."domainId" = p_domainId
        AND LENGTH(r."url") > 0
        AND r."rule" = FALSE
        AND dq."url" ILIKE '%' || r."url" || '%'
    );

    DELETE FROM public."Downloads" WHERE "id" IN (
        SELECT DISTINCT d."id"
        FROM public."Downloads" d
        JOIN public."DownloadRules" r ON r."domainId" = d."domainId"
        WHERE d."domainId" = p_domainId
        AND LENGTH(r."url") > 0
        AND r."rule" = FALSE
        AND d."url" ILIKE '%' || r."url" || '%'
    );

    DROP TABLE IF EXISTS tmp_articles_to_clean;
END;
$$;