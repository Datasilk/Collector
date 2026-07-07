CREATE OR REPLACE FUNCTION public."ArticleWord_Add"
(
    p_articleId INT DEFAULT 0,
    p_wordId INT DEFAULT 0,
    p_count INT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    IF (SELECT COUNT(*) FROM public."ArticleWords" a WHERE a."articleId" = p_articleId AND a."wordId" = p_wordId) = 0 THEN
        INSERT INTO public."ArticleWords" ("articleId", "wordId", "count")
        VALUES (p_articleId, p_wordId, p_count);
    END IF;
END;
$$;