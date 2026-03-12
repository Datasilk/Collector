CREATE OR REPLACE PROCEDURE  public."Article_UpdateUrl"
(
    IN articleId INT DEFAULT 0,
    IN url VARCHAR(250),
    IN domain VARCHAR(250),
    IN parentId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    oldurl VARCHAR(250), domainId INT;
    newarticleId INT;
BEGIN
SELECT oldurl="url" FROM Articles WHERE articleId=articleId
SELECT domainId=domainId FROM Domains WHERE domain=domain
IF domainId IS NULL BEGIN
	EXEC Domain_Add domain=domain, parentId=parentId
	SELECT domainId=domainId FROM Domains WHERE domain=domain
END
IF oldurl != url BEGIN
	SELECT TOP 1 newarticleId = articleId FROM Articles WHERE "url"=url ORDER BY datecreated ASC
	IF newarticleId IS NOT NULL AND newarticleId != articleId BEGIN
		DELETE FROM Articles WHERE articleId=articleId
	END
	UPDATE Articles SET "url"=url, domainId=domainId, domain=domain WHERE articleId=articleId
	--delete any downloads that already use the new URL
	DELETE FROM DownloadQueue WHERE "url"=url
	DELETE FROM Downloads WHERE "url"=url
	--update downloads that used the old URL
	UPDATE DownloadQueue SET "url"=url, domainId=domainId WHERE "url"=oldurl
	UPDATE Downloads SET "url"=url, domainId=domainId WHERE "url"=oldurl
END
END;

$$;
