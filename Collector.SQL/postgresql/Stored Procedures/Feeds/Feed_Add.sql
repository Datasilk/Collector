CREATE OR REPLACE PROCEDURE  public."Feed_Add"
(
    IN doctype INT DEFAULT 1,
    IN categoryId INT,
    IN title VARCHAR(100) DEFAULT '',
    IN url VARCHAR(100) DEFAULT '',
    IN domain VARCHAR(64) DEFAULT '',
    IN filter TEXT DEFAULT '',
    IN checkIntervals INT DEFAULT 720 --(12 hours)
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT;
    feedId INT := nextval('public."SequenceFeeds"');
BEGIN
IF NOT EXISTS(SELECT * FROM Domains WHERE domain=domain) BEGIN
	--get domain ID
	SELECT domainId = domainId, title = title FROM Domains WHERE domain=domain
END ELSE BEGIN
	--create domain ID
	SET domainId = nextval('public."SequenceDomains"')
	INSERT INTO Domains (domainId, parentId, domain, lastchecked) VALUES (domainId, 0, domain, DATEADD(HOUR, -1, CURRENT_TIMESTAMP))
END
INSERT INTO Feeds (feedId, domainId, doctype, categoryId, title, url, checkIntervals, filter, lastChecked) 
VALUES (feedId, domainId, doctype, categoryId, title, url, checkIntervals, filter, DATEADD(HOUR, -24, CURRENT_TIMESTAMP))
BEGIN TRY
	INSERT INTO Whitelist_Domains (domain) VALUES (domain)
END TRY
BEGIN CATCH
END CATCH
SELECT feedId
END;

$$;
