DELETE FROM Articles WHERE articleId IN (
	SELECT DISTINCT c.articleId --, a.url, b.total
	FROM Articles a
	CROSS APPLY (
		SELECT COUNT(*) AS total FROM Articles WHERE url=a.url
	) b
	CROSS APPLY (
		SELECT TOP 1 articleId FROM Articles WHERE url=a.url ORDER BY datecreated DESC
	) c
	WHERE b.total > 1
)

DELETE FROM DownloadQueue WHERE qId IN (
	SELECT DISTINCT c.qId --, a.url, b.total
	FROM DownloadQueue a
	CROSS APPLY (
		SELECT COUNT(*) AS total FROM DownloadQueue WHERE url=a.url
	) b
	CROSS APPLY (
		SELECT TOP 1 qId FROM DownloadQueue WHERE url=a.url ORDER BY datecreated DESC
	) c
	WHERE b.total > 1
)

DELETE FROM Downloads WHERE id IN (
	SELECT DISTINCT c.id --, a.url, b.total
	FROM Downloads a
	CROSS APPLY (
		SELECT COUNT(*) AS total FROM Downloads WHERE url=a.url
	) b
	CROSS APPLY (
		SELECT TOP 1 id FROM Downloads WHERE url=a.url ORDER BY datecreated DESC
	) c
	WHERE b.total > 1
)