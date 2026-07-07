CREATE OR REPLACE FUNCTION public."Words_BulkAdd"
(
    p_words TEXT,
    p_subjectId INT DEFAULT 0,
    p_grammartype INT DEFAULT 0,
    p_score INT DEFAULT 1
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_word VARCHAR(32);
    v_wordId INT;
BEGIN
    FOR v_word IN SELECT DISTINCT TRIM(unnest(string_to_array(p_words, ','))) LOOP
        IF v_word = '' THEN CONTINUE; END IF;

        SELECT w."wordId" INTO v_wordId FROM public."Words" w WHERE w."word" = v_word LIMIT 1;

        IF v_wordId IS NULL THEN
            v_wordId := nextval('public."SequenceWords"');
            INSERT INTO public."Words" ("wordId", "word", "grammartype", "score")
            VALUES (v_wordId, v_word, p_grammartype, p_score);
        END IF;

        IF NOT EXISTS(SELECT 1 FROM public."SubjectWords" sw WHERE sw."subjectId" = p_subjectId AND sw."wordId" = v_wordId) THEN
            INSERT INTO public."SubjectWords" ("wordId", "subjectId") VALUES (v_wordId, p_subjectId);
        END IF;
    END LOOP;
END;
$$;