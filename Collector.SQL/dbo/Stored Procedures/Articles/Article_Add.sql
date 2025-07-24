CREATE PROCEDURE [dbo].[Article_Add]
	@feedId int = 0,
	@subjects int = 0,
	@subjectId int = 0,
	@score smallint = 0,
	@domain nvarchar(50),
	@url nvarchar(250),
	@title nvarchar(250) = '',
	@summary nvarchar(250) = '',
	@filesize float = 0,
	@linkcount int = 0,
	@linkwordcount int = 0,
	@wordcount int = 0,
	@sentencecount smallint = 0,
	@paragraphcount smallint = 0,
	@importantcount smallint = 0,
	@yearstart smallint = 0,
	@yearend smallint = 0,
	@years nvarchar(50) = '',
	@images smallint = 0,
	@datepublished datetime = NULL,
	@relavance smallint = 1,
	@importance smallint = 1,
	@fiction smallint = 1,
	@analyzed float = 0.1,
	@active bit = 1
AS
SET NOCOUNT ON

DECLARE @articleId int = NULL
DECLARE @domainId int = NULL
SELECT @domainId = domainId FROM Domains WHERE domain=@domain
SELECT @articleId = articleId FROM Articles WHERE url=@url

IF @domainId IS NULL BEGIN
	DECLARE @domain_results TABLE (id int)
	INSERT INTO @domain_results
	EXEC Domain_Add @domain=@domain, @parentId=0
	SELECT @domainId = domainId FROM Domains WHERE domain=@domain
END

IF @articleId IS NULL BEGIN
	SET @articleId = NEXT VALUE FOR SequenceArticles
	INSERT INTO Articles 
	(articleId, feedId, subjects, subjectId, domainId, score, domain, url, title, summary, filesize, linkcount, linkwordcount, wordcount, sentencecount, paragraphcount, importantcount, analyzecount,
	yearstart, yearend, years, images, datecreated, datepublished, relavance, importance, fiction, analyzed, active)
	VALUES 
	(@articleId, @feedId, @subjects, @subjectId, @domainId, @score, @domain, @url, @title, @summary, @filesize, @linkcount, @linkwordcount, @wordcount, @sentencecount, @paragraphcount, @importantcount, 1,
	@yearstart, @yearend, @years, @images, GETUTCDATE(), @datepublished, @relavance, @importance, @fiction, @analyzed, @active)
	
	--update domain record
	UPDATE Domains SET articles+=1 WHERE domainId=@domainId
END

--archive related Download Queue record
UPDATE DownloadQueue SET status=0 WHERE [url] = @url

SELECT @articleId
