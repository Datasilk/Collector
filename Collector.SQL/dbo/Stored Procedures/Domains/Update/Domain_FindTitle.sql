CREATE PROCEDURE [dbo].[Domain_FindTitle]
	@domainId int = 0
AS
	SET NOCOUNT ON;
	--get common word found in all article titles
	DECLARE @articles TABLE (
		title nvarchar(250)
	)
	DECLARE @words TABLE (
		word nvarchar(MAX)
	)

	INSERT INTO @articles 
	SELECT top 100 a.title FROM Articles a
	WHERE a.domainId = @domainId

	DECLARE @count int = 0
	SELECT @count = COUNT(*) FROM @articles
	DECLARE @exclude TABLE(value NVARCHAR(32))
	INSERT INTO @exclude ([value])
	VALUES ('and'), ('or'), ('&'), ('the'), ('for'), ('with')

	DECLARE @cursor CURSOR, @title nvarchar(250)
	SET @cursor = CURSOR FOR
	SELECT title FROM @articles
	OPEN @cursor
	DECLARE @i int = 0
	FETCH NEXT FROM @cursor INTO @title
	WHILE @@FETCH_STATUS = 0 BEGIN
		--get all words & phrases from the title
		INSERT INTO @words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(@title, ' ')) as tbl WHERE LEN(value) > 2 AND [value] NOT IN (SELECT * FROM @exclude)
		INSERT INTO @words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(@title, '-')) as tbl WHERE LEN(value) > 2 AND [value] NOT IN (SELECT * FROM @exclude)
		INSERT INTO @words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(@title, '|')) as tbl WHERE LEN(value) > 2 AND [value] NOT IN (SELECT * FROM @exclude)
		INSERT INTO @words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(@title, ':')) as tbl WHERE LEN(value) > 2 AND [value] NOT IN (SELECT * FROM @exclude)
		INSERT INTO @words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(@title, ';')) as tbl WHERE LEN(value) > 2 AND [value] NOT IN (SELECT * FROM @exclude)
		INSERT INTO @words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(@title, '/')) as tbl WHERE LEN(value) > 2 AND [value] NOT IN (SELECT * FROM @exclude)
		FETCH NEXT FROM @cursor INTO @title
	END
	CLOSE @cursor
	DEALLOCATE @cursor
	SELECT @count = COUNT(*) FROM @words

	--get count of all duplicate words & phrases
	DECLARE @domain nvarchar(64), @domainpart nvarchar(64), @domainpart2 nvarchar(64)
	DECLARE @domainparts TABLE (value nvarchar(64))
	DECLARE @domainTitle nvarchar(128)
	SELECT @domain = domain FROM Domains WHERE domainId=@domainId
	INSERT INTO @domainparts SELECT * FROM STRING_SPLIT(@domain, '.')
	SELECT TOP 1 @domainpart = REPLACE([value], '-', '%') FROM @domainparts
	SELECT @domainpart2 = STRING_AGG([value], '') FROM @domainparts
	--PRINT @domainpart2
	SELECT TOP 1 @domainTitle = TRIM(word)
	FROM (
		SELECT b.score, w.word, COUNT(w.word) AS total, LEN(w.word) AS [length]
		FROM @words w
		CROSS APPLY (
			SELECT CASE 
			WHEN PATINDEX(@domainpart + '%', REPLACE(w.word, ' ', '')) > 0 THEN 50 
			WHEN PATINDEX(@domainpart2 + '%', REPLACE(w.word, ' ', '')) > 0 THEN 100
			ELSE 0 END AS score
		) AS b
		GROUP BY w.word, b.score
		HAVING COUNT(w.word) > 1
	) AS tbl
	ORDER BY score DESC, total DESC, [length] DESC

	UPDATE Domains SET title=@domainTitle, hastitle=1, dateupdated = GETUTCDATE() WHERE domainId=@domainId
	
	SELECT @domainTitle