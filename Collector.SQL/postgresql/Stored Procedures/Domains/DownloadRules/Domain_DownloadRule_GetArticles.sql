CREATE OR REPLACE PROCEDURE  public."Domain_DownloadRule_GetArticles"
(
    IN ruleId INT
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT, url VARCHAR(64), title VARCHAR(64), summary VARCHAR(64);
BEGIN
SELECT domainId = domainId, url = "url", title = "title", summary = "summary" FROM DownloadRules WHERE ruleId = ruleId
	SELECT articleId
	FROM Articles 
	WHERE domainId=domainId 
	AND (
		"url" LIKE url
		OR (LEN(title) > 0 AND title LIKE title)
		OR (LEN(summary) > 0 AND summary LIKE summary)
	);
END;

$$;
