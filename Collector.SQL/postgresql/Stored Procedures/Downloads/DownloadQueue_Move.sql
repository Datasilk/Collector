CREATE OR REPLACE PROCEDURE  public."DownloadQueue_Move"
(
    IN qid bigint DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
--move related Download Queue record into Downloads table
	INSERT INTO Downloads ("id", "feedId", "domainId", "status", "type", "tries", "url", "path", "datecreated") 
	SELECT * FROM DownloadQueue WHERE qid=qid
	DELETE FROM DownloadQueue WHERE qid=qid
END;

$$;
