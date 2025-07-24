CREATE PROCEDURE [dbo].[Word_Add]
	@word nvarchar(64),
	@subjectId int = 0,
	@grammartype int = 0,
	@score int = 1
AS
	DECLARE @wordId int
	IF(SELECT COUNT(*) FROM Words WHERE word=@word AND grammartype=@grammartype) = 0 BEGIN
		/* word doesn't exists */
		SET @wordId = NEXT VALUE FOR SequenceWords
		INSERT INTO Words (wordId, word, grammartype, score) 
		VALUES (@wordId, @word, @grammartype, @score)
	END ELSE BEGIN
		SELECT @wordId = wordId FROM Words WHERE word=@word
	END

	IF @wordId IS NOT NULL BEGIN
		INSERT INTO SubjectWords (wordId, subjectId) VALUES (@wordId, @subjectId)
	END
	
	SELECT @wordId
