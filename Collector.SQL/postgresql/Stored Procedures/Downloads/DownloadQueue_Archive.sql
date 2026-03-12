CREATE OR REPLACE PROCEDURE  public."DownloadQueue_Archive"
(
    IN qid bigint DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT;
BEGIN
SELECT domainId=domainId FROM DownloadQueue WHERE qid=qid
	UPDATE DownloadQueue SET status=2 WHERE qid=qid
	UPDATE Domains SET inqueue-=1 WHERE domainId=domainId
END;

$$;
