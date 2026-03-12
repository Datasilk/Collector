CREATE OR REPLACE PROCEDURE  public."Download_Update"
(
    IN qid bigint DEFAULT 0,
    IN status INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE DownloadQueue SET status=status WHERE qid=qid
END;

$$;
