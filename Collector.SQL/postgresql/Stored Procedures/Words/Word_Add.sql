CREATE OR REPLACE PROCEDURE  public."Word_Add"
(
    IN word VARCHAR(64),
    IN subjectId INT DEFAULT 0,
    IN grammartype INT DEFAULT 0,
    IN score INT DEFAULT 1
);
LANGUAGE plpgsql
AS $$
DECLARE
    wordId INT;
BEGIN
IF(SELECT COUNT(*) FROM Words WHERE word=word AND grammartype=grammartype) = 0 BEGIN
		/* word doesn't exists */
		SET wordId = nextval('public."SequenceWords"')
		INSERT INTO Words (wordId, word, grammartype, score) 
		VALUES (wordId, word, grammartype, score)
	END ELSE BEGIN
		SELECT wordId = wordId FROM Words WHERE word=word
	END
	IF wordId IS NOT NULL BEGIN
		INSERT INTO SubjectWords (wordId, subjectId) VALUES (wordId, subjectId)
	END
	SELECT wordId
END;

$$;
