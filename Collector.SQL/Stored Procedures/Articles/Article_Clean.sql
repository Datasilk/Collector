CREATE OR REPLACE FUNCTION public."Article_Clean"
(
    p_articleId INT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    PERFORM public."ArticleSubjects_Remove"(p_articleId);
    PERFORM public."ArticleWords_Remove"(p_articleId);
END;
$$;