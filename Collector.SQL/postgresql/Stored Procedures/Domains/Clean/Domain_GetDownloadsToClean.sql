CREATE OR REPLACE PROCEDURE  public."Domain_GetDownloadsToClean"
(
    IN domainId INT,
    IN topten BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
DECLARE
    articles TABLE (articleId INT);
    a_total INT, b_total INT;
BEGIN
--get all article that will be deleted
	INSERT INTO articles 
	SELECT DISTINCT a.articleId
	FROM Articles a
	JOIN DownloadRules r ON r.domainId = a.domainId
	WHERE a.domainId=domainId 
	AND 
	(
		( -- download rules
			(LEN(r."url") > 0 AND a."url" LIKE '%' + r."url" + '%')
			OR (LEN(r.title) > 0 AND a.title LIKE '%' + r.title + '%')
			OR (LEN(r.summary) > 0 AND a.summary LIKE '%' + r.summary + '%')
		);
	);
	-- #1: count of all affected articles
	SELECT COUNT(*) AS total FROM articles
	-- #2: details about affected articles
	IF topten = 1 BEGIN
		SELECT TOP 10 articleId, "title", "url" FROM Articles WHERE articleId IN (SELECT * FROM articles)
	END ELSE BEGIN
		SELECT articleId, "title", "url" FROM Articles WHERE articleId IN (SELECT * FROM articles)
	END
	-- #3: total affected downloads from download queue & download archive
	SELECT a_total = COUNT(*) FROM (
		SELECT DISTINCT dq.qid FROM DownloadQueue dq
		JOIN DownloadRules r ON r.domainId = dq.domainId
		WHERE dq.domainId=domainId 
		AND (
			(LEN(r."url") > 0 AND r."rule"=0 AND dq."url" LIKE '%' + r."url" + '%')
		);
	) AS tbl
	SELECT b_total = COUNT(*) FROM (
		SELECT DISTINCT d.id FROM Downloads d
		JOIN DownloadRules r ON r.domainId = d.domainId
		WHERE d.domainId=domainId 
		AND (
			(LEN(r."url") > 0 AND r."rule"=0 AND d."url" LIKE '%' + r."url" + '%')
		);
	) AS tbl
	SELECT a_total + b_total AS total
END;

$$;
