CREATE PROCEDURE [dbo].[Article_Update]
	@articleId int = 0,
	@subjects int = 0,
	@subjectId int = 0,
	@score smallint = 0,
	@title nvarchar(250),
	@summary nvarchar(250),
	@filesize float = 0,
	@wordcount int = 0,
	@sentencecount int = 0,
	@paragraphcount int = 0,
	@importantcount int = 0,
	@yearstart int = 0,
	@yearend int = 0,
	@years nvarchar(50),
	@images smallint = 0,
	@datepublished datetime,
	@relavance smallint = 1,
	@importance smallint = 1,
	@fiction smallint = 1,
	@analyzed float = 0.1
AS

UPDATE Articles SET 
subjects=@subjects, subjectId=@subjectId, score=@score, title=@title, summary=@summary, filesize=@filesize, wordcount=@wordcount, sentencecount=@sentencecount,
paragraphcount=@paragraphcount, importantcount=@importantcount, analyzecount=analyzecount+1, 
yearstart=@yearstart, yearend=@yearend, years=@years, images=@images, datepublished=@datepublished, 
relavance=@relavance, importance=@importance, fiction=@fiction, analyzed=@analyzed
WHERE articleId=@articleId
