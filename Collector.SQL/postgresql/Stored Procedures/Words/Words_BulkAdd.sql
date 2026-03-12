CREATE OR REPLACE PROCEDURE  public."Words_BulkAdd"
(
    IN words TEXT,
    IN subjectId INT DEFAULT 0,
    IN grammartype INT DEFAULT 0,
    IN score INT DEFAULT 1
);
LANGUAGE plpgsql
AS $$
DECLARE
    word VARCHAR(32), wordId INT, cursor CURSOR;
BEGIN
SELECT "value" as word INTO #words FROM public.SplitArray(words, ',')
	SET cursor = CURSOR FOR
	SELECT word FROM #words
	OPEN cursor
	FETCH NEXT FROM cursor INTO word
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF NOT EXISTS(SELECT * FROM Words WHERE word=word) BEGIN
			/* word doesn't exists */
			SET wordId = nextval('public."SequenceWords"')
			INSERT INTO Words (wordId, word, grammartype, score) 
			VALUES (wordId, word, grammartype, score)
		END ELSE BEGIN
			SELECT wordId = wordId FROM Words WHERE word=word
		END
		IF wordId IS NOT NULL AND wordId > 0 
		AND NOT EXISTS(SELECT * FROM SubjectWords WHERE subjectId=subjectId AND wordId=wordId) BEGIN
			INSERT INTO SubjectWords (wordId, subjectId) VALUES (wordId, subjectId)
		END
		FETCH NEXT FROM cursor INTO word
	END
	CLOSE cursor
	DEALLOCATE cursor
END;

$$;
