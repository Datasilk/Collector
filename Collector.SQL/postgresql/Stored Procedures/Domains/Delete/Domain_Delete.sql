CREATE OR REPLACE PROCEDURE  public."Domain_Delete"
(
    IN domainId INT
);
LANGUAGE plpgsql
AS $$
DECLARE
    domain VARCHAR(128);
BEGIN
SELECT domain = domain FROM Domains WHERE domainId=domainId
	EXEC Domain_DeleteAllArticles domainId=domainId
	DELETE FROM Domains WHERE domainId=domainId
	DELETE FROM DownloadQueue WHERE domainId=domainId
	DELETE FROM Downloads WHERE domainId=domainId
	DELETE FROM Whitelist_Domains WHERE domain=domain
	DELETE FROM Blacklist_Domains WHERE domain=domain
END;

$$;
