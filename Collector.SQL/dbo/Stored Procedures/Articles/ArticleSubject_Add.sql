CREATE PROCEDURE [dbo].[ArticleSubject_Add]
	@articleId int = 0,
	@subjectId int = 0,
	@datepublished datetime = null,
	@score int = 0
AS
	IF (SELECT COUNT(*) FROM ArticleSubjects WHERE articleId=@articleId AND subjectId=@subjectId) = 0 BEGIN
		INSERT INTO ArticleSubjects (articleId, subjectId, datecreated, datepublished, score) 
		VALUES (@articleId, @subjectId, GETUTCDATE(), @datepublished, @score)
	END
