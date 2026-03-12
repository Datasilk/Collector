CREATE OR REPLACE PROCEDURE  public."Blacklist_Domain_Add"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT;
BEGIN TRY
	INSERT INTO Blacklist_Domains (domain) VALUES (domain)
	END TRY
	BEGIN CATCH
	END CATCH
	SELECT domainId=domainId FROM Domains WHERE domain=domain
	-- delete all articles related to domain
	EXEC Domain_DeleteAllArticles domainId=domainId
	--delete all download queue related to domain
	DELETE FROM DownloadQueue WHERE domainId=domainId
	DELETE FROM Downloads WHERE domainId=domainId
	DELETE FROM Domains WHERE domainId=domainId
	--delete whitelisted domains (if any)
	DELETE FROM Whitelist_Domains WHERE domain=domain
END;

$$;
