CREATE PROCEDURE [dbo].[Feeds_Categories_GetList]
AS
	SELECT * FROM FeedCategories ORDER BY title ASC
