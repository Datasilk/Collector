CREATE OR REPLACE FUNCTION public."Word_Add"
(
    p_word VARCHAR(64),
    p_subjectId INT DEFAULT 0,
    p_grammartype INT DEFAULT 0,
    p_score INT DEFAULT 1
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_wordId INT;
BEGIN
    IF (SELECT COUNT(*) FROM public."Words" w WHERE w."word" = p_word AND w."grammartype" = p_grammartype) = 0 THEN
        v_wordId := nextval('public."SequenceWords"');
        INSERT INTO public."Words" ("wordId", "word", "grammartype", "score")
        VALUES (v_wordId, p_word, p_grammartype, p_score);
    ELSE
        SELECT w."wordId" INTO v_wordId FROM public."Words" w WHERE w."word" = p_word LIMIT 1;
    END IF;

    IF v_wordId IS NOT NULL THEN
        BEGIN
            INSERT INTO public."SubjectWords" ("wordId", "subjectId") VALUES (v_wordId, p_subjectId);
        EXCEPTION
            WHEN unique_violation THEN
                NULL;
        END;
    END IF;

    RETURN v_wordId;
END;
$$;