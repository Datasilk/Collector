CREATE OR REPLACE FUNCTION public."ArticleSentence_Add"
(
    p_articleId INT DEFAULT 0,
    p_index INT DEFAULT 0,
    p_sentence TEXT DEFAULT ''
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public."ArticleSentences" ("articleId", "index", "sentence")
    VALUES (p_articleId, p_index, p_sentence);
END;
$$;