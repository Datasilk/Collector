CREATE OR REPLACE PROCEDURE  public."Download_Delete"
(
    IN qid bigint DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    url VARCHAR(250), domainId INT;
BEGIN
SELECT url = "url", domainId=domainId FROM DownloadQueue WHERE qid=qid
	--delete the article associated with download
	DELETE FROM Articles WHERE "url" = (SELECT "url" FROM DownloadQueue WHERE qid=qid)
	DELETE FROM DownloadQueue WHERE qid=qid
	DELETE FROM Downloads WHERE id=qid
	UPDATE Domains SET inqueue-=1 WHERE domainId=domainId
END;

$$;
