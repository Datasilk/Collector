CREATE OR REPLACE FUNCTION public."ArticleSubjects_Remove"
(
    p_articleId INT DEFAULT 0,
    p_subjectId INT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_subjectId = 0 THEN
        DELETE FROM public."ArticleSubjects" WHERE "articleId" = p_articleId;
    ELSE
        DELETE FROM public."ArticleSubjects" WHERE "articleId" = p_articleId AND "subjectId" = p_subjectId;
    END IF;
END;
$$;