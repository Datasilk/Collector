CREATE OR REPLACE PROCEDURE public."DownloadQueue_MoveArchived"
LANGUAGE plpgsql
AS $$
BEGIN
INSERT INTO Downloads ("id", "feedId", "domainId", "type", "status", "tries", "url", "path", "datecreated") 
	SELECT * FROM DownloadQueue WHERE "status"=2 AND NOT EXISTS(SELECT * FROM Downloads WHERE id=qid)
	DELETE FROM DownloadQueue WHERE "status"=2
END;

$$;
