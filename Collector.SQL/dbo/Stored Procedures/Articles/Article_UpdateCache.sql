CREATE PROCEDURE [dbo].[Article_UpdateCache]
	@articleId int = 0,
	@cached bit = 1
AS
	UPDATE Articles SET cached=@cached WHERE articleId=@articleId
