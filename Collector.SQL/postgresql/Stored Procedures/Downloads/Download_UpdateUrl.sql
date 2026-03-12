CREATE OR REPLACE PROCEDURE  public."Download_UpdateUrl"
(
    IN qId bigint DEFAULT 0,
    IN url VARCHAR(250),
    IN domain VARCHAR(250)
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT;
BEGIN
SELECT domainId=domainId FROM Domains WHERE domain=domain
	IF domainId IS NULL BEGIN
		SET domainId = nextval('public."SequenceDomains"')
		INSERT INTO Domains (domainId, domain) VALUES (domainId, domain)
	END
	IF EXISTS(SELECT * FROM DownloadQueue WHERE url=url) BEGIN
		--remove existing download queue item
		DELETE FROM DownloadQueue WHERE url=url
	END
	UPDATE DownloadQueue SET "url"=url, domainId=domainId WHERE qid=qid
	IF EXISTS(SELECT * FROM Downloads WHERE url=url) BEGIN
		DELETE FROM Downloads WHERE url=url
	END
	UPDATE Downloads SET "url"=url, domainId=domainId WHERE id=qid
END;

$$;
