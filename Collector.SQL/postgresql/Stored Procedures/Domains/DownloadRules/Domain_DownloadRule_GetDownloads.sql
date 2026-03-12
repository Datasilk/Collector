CREATE OR REPLACE PROCEDURE  public."Domain_DownloadRule_GetDownloads"
(
    IN ruleId INT
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT, url VARCHAR(64);
BEGIN
SELECT domainId = domainId, url = "url" FROM DownloadRules WHERE ruleId = ruleId
	SELECT qid
	FROM DownloadQueue 
	WHERE domainId=domainId 
	AND (
		"url" LIKE url
	);
END;

$$;
