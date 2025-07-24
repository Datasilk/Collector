CREATE PROCEDURE [dbo].[Words_BulkAdd]
	@words nvarchar(MAX),
	@subjectId int = 0,
	@grammartype int = 0,
	@score int = 1
AS
	SELECT [value] as word INTO #words FROM dbo.SplitArray(@words, ',')
	DECLARE @word nvarchar(32), @wordId int, @cursor CURSOR
	SET @cursor = CURSOR FOR
	SELECT word FROM #words
	OPEN @cursor
	FETCH NEXT FROM @cursor INTO @word
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF NOT EXISTS(SELECT * FROM Words WHERE word=@word) BEGIN
			/* word doesn't exists */
			SET @wordId = NEXT VALUE FOR SequenceWords
			INSERT INTO Words (wordId, word, grammartype, score) 
			VALUES (@wordId, @word, @grammartype, @score)
		END ELSE BEGIN
			SELECT @wordId = wordId FROM Words WHERE word=@word
		END

		IF @wordId IS NOT NULL AND @wordId > 0 
		AND NOT EXISTS(SELECT * FROM SubjectWords WHERE subjectId=@subjectId AND wordId=@wordId) BEGIN
			INSERT INTO SubjectWords (wordId, subjectId) VALUES (@wordId, @subjectId)
		END

		FETCH NEXT FROM @cursor INTO @word
	END
	CLOSE @cursor
	DEALLOCATE @cursor
