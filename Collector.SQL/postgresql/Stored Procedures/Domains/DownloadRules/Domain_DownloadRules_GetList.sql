CREATE OR REPLACE PROCEDURE  public."Domain_DownloadRules_GetList"
(
    IN domainId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM DownloadRules WHERE domainId=domainId ORDER BY datecreated ASC
END;

$$;
