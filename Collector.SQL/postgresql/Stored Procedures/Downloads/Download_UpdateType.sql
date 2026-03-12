CREATE OR REPLACE PROCEDURE  public."Download_UpdateType"
(
    IN qId BIGINT,
    IN type SMALLINT
);
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."DownloadQueue"
    SET "type" = type
    WHERE "qid" = qId
END

$$;
