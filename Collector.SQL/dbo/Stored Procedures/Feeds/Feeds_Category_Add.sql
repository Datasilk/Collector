CREATE PROCEDURE [dbo].[Feeds_Category_Add]
	@title nvarchar(64)
AS

	DECLARE @id int = NEXT VALUE FOR SequenceFeedCategories
	INSERT INTO FeedCategories (categoryId, title) VALUES (@id, @title)
