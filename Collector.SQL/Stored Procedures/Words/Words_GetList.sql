CREATE OR REPLACE FUNCTION public."Words_GetList"
(
    p_words TEXT
)
RETURNS TABLE("wordId" INT, "word" VARCHAR(64), "grammartype" INT, "score" INT)
LANGUAGE plpgsql
AS $$
DECLARE
    v_words TEXT[] := string_to_array(p_words, ',');
BEGIN
    RETURN QUERY
    SELECT w.* FROM public."Words" w
    WHERE w."word" = ANY(v_words);
END;
$$;