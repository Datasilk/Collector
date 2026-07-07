CREATE OR REPLACE FUNCTION public."ArticleSubject_Add"
(
    p_articleId INT DEFAULT 0,
    p_subjectId INT DEFAULT 0,
    p_datePublished TIMESTAMP DEFAULT NULL,
    p_score INT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    IF (SELECT COUNT(*) FROM public."ArticleSubjects" a WHERE a."articleId" = p_articleId AND a."subjectId" = p_subjectId) = 0 THEN
        INSERT INTO public."ArticleSubjects" ("articleId", "subjectId", "datecreated", "datepublished", "score")
        VALUES (p_articleId, p_subjectId, CURRENT_TIMESTAMP, p_datePublished, p_score);
    END IF;
END;
$$;