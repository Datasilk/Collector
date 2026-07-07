CREATE OR REPLACE FUNCTION public."Domain_DeleteAllArticles"
(
    p_domainId INT
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public."ArticleSentences" WHERE "articleId" IN (
        SELECT a."articleId" FROM public."Articles" a WHERE a."domainId" = p_domainId
    );
    DELETE FROM public."ArticleWords" WHERE "articleId" IN (
        SELECT a."articleId" FROM public."Articles" a WHERE a."domainId" = p_domainId
    );
    DELETE FROM public."ArticleSubjects" WHERE "articleId" IN (
        SELECT a."articleId" FROM public."Articles" a WHERE a."domainId" = p_domainId
    );
    DELETE FROM public."Articles" WHERE "domainId" = p_domainId;
END;
$$;