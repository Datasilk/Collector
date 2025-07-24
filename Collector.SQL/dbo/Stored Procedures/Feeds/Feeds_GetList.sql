CREATE PROCEDURE [dbo].[Feeds_GetList]
AS
SELECT f.*, fc.title AS category
FROM Feeds f
JOIN FeedCategories fc ON fc.categoryId = f.categoryId
WHERE feedId > 0 ORDER BY fc.title ASC, f.title ASC
