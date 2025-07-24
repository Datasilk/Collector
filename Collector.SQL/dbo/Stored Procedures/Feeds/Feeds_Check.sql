CREATE PROCEDURE [dbo].[Feeds_Check]
	@feedId int = 0
AS
	SELECT f.*, c.title AS category
	FROM Feeds f 
	JOIN FeedCategories c ON c.categoryId = f.categoryId
	WHERE f.lastChecked < DATEADD(MINUTE, -1 * f.checkIntervals, GETUTCDATE())
	AND (
		(@feedId > 0 AND f.feedId = @feedId)
		OR @feedId = 0
	)
