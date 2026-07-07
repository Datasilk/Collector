CREATE OR REPLACE FUNCTION public."CommonWords_Add"
(
    p_words TEXT
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_words TEXT[] := string_to_array(p_words, ',');
BEGIN
    DELETE FROM public."CommonWords" WHERE "word" = ANY(v_words);
    INSERT INTO public."CommonWords" ("word") SELECT DISTINCT unnest(v_words);
END;
$$;