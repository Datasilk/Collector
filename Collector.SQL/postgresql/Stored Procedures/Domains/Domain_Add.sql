CREATE OR REPLACE PROCEDURE  public."Domain_Add"
(
    IN domain VARCHAR(64),
    IN title VARCHAR(128) DEFAULT '',
    IN parentId INT DEFAULT 0,
    IN type INT DEFAULT 0 -- 0 = none, 1 = whitelist, 2 = blacklist
);
LANGUAGE plpgsql
AS $$
DECLARE
    id INT := nextval('public."SequenceDomains"');
    level INT;
    url TEXT := 'http://' + domain;
BEGIN
INSERT INTO Domains (domainId, parentId, domain, title, lastchecked)
	VALUES (id, parentId, domain, title, DATEADD(HOUR, -1, CURRENT_TIMESTAMP))
	SELECT id
	IF parentId > 0 BEGIN
		BEGIN TRY
			INSERT INTO DomainHierarchy (domainId, parentId, "level")
			SELECT id, parentId, "level"
			FROM DomainHierarchy WHERE domainId = parentId
		END TRY BEGIN CATCH END CATCH
		SELECT level = ISNULL(MAX("level"), 0) + 1 FROM DomainHierarchy WHERE domainId = parentId
		BEGIN TRY
			INSERT INTO DomainHierarchy (domainId, parentId, "level")
			VALUES (id, parentId, level)
		END TRY BEGIN CATCH END CATCH
		EXEC DomainLink_Add domainId=parentId, linkId=id
	END
	IF type = 1 EXEC Whitelist_Domain_Add domain=domain
	IF type = 2 EXEC Blacklist_Domain_Add domain=domain
	EXEC DownloadQueue_Add url=url, domain=domain, parentId=parentId, feedId=0
END;

$$;
