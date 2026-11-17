CREATE OR REPLACE FUNCTION public."DomainTypeMatches_Add"
(
    p_type INT,
    p_type2 INT DEFAULT -1,
    p_words TEXT DEFAULT '',
    p_threshold INT DEFAULT 0,
    p_rank INT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_id INT;
BEGIN
    v_id := nextval('public."SequenceDomainTypeMatches"');

    INSERT INTO public."DomainTypeMatches" ("matchId", "type", "type2", "words", "threshold", "rank")
    VALUES (v_id, p_type, p_type2, p_words, p_threshold, p_rank);
END;
$$;