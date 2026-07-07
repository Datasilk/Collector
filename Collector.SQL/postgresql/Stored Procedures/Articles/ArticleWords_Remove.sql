CREATE OR REPLACE FUNCTION public."ArticleWords_Remove"
(
    p_articleId INT DEFAULT 0,
    p_word VARCHAR(50) DEFAULT ''
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_wordId INT := 0;
BEGIN
    IF p_word = '' THEN
        DELETE FROM public."ArticleWords" WHERE "articleId" = p_articleId;
    ELSE
        SELECT w."wordId" INTO v_wordId FROM public."Words" w WHERE w."word" = p_word;
        DELETE FROM public."ArticleWords" WHERE "articleId" = p_articleId AND "wordId" = v_wordId;
    END IF;
END;
$$;