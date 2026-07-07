CREATE OR REPLACE FUNCTION public."Article_Remove"
(
    p_articleId INT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_domainId INT;
BEGIN
    SELECT a."domainId" INTO v_domainId
    FROM public."Articles" a
    WHERE a."articleId" = p_articleId;

    IF v_domainId IS NOT NULL THEN
        DELETE FROM public."ArticleSentences" WHERE "articleId" = p_articleId;
        DELETE FROM public."ArticleWords" WHERE "articleId" = p_articleId;
        DELETE FROM public."ArticleSubjects" WHERE "articleId" = p_articleId;
        DELETE FROM public."Articles" WHERE "articleId" = p_articleId;
        UPDATE public."Domains" SET "articles" = "articles" - 1 WHERE "domainId" = v_domainId;
    END IF;
END;
$$;