CREATE PROCEDURE [dbo].[Article_Visited]
	@articleId int
AS
	UPDATE Articles SET visited += 1, cached = 1 WHERE articleId=@articleId
