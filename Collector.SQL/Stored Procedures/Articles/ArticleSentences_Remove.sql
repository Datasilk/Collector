CREATE OR REPLACE FUNCTION public."ArticleSentences_Remove"
(
    p_articleId INT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public."ArticleSentences" WHERE "articleId" = p_articleId;
END;
$$;