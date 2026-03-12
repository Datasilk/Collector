CREATE OR REPLACE PROCEDURE  public."DomainTypeMatches_Add"
(
    IN type INT,
    IN type2 INT DEFAULT -1,
    IN words TEXT,
    IN threshold INT,
    IN rank INT
);
LANGUAGE plpgsql
AS $$
DECLARE
    id INT;
BEGIN
SET id = nextval('public."SequenceDomainTypeMatches"')
	INSERT INTO DomainTypeMatches (matchId, "type", "type2", words, threshold, "rank")
	VALUES (id, type, type2, words, threshold, rank)
END;

$$;
