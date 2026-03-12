CREATE OR REPLACE PROCEDURE  public."Feeds_Check"
(
    IN feedId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT f.*, c.title AS category
	FROM Feeds f 
	JOIN FeedCategories c ON c.categoryId = f.categoryId
	WHERE f.lastChecked < DATEADD(MINUTE, -1 * f.checkIntervals, CURRENT_TIMESTAMP)
	AND (
		(feedId > 0 AND f.feedId = feedId)
		OR feedId = 0
	);
END;

$$;
