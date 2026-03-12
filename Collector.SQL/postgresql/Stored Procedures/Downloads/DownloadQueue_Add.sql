CREATE OR REPLACE PROCEDURE  public."DownloadQueue_Add"
(
    IN url TEXT DEFAULT '',
    IN domain VARCHAR(64) DEFAULT '',
    IN parentId INT,
    IN feedId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT, qid BIGINT, count INT := 0, title VARCHAR(128);
    title_results TABLE (title TEXT);
    domain_results TABLE (id INT);
BEGIN
IF EXISTS(SELECT * FROM Domains WHERE domain=domain) BEGIN
	--get domain ID
	SELECT domainId = domainId, title = title FROM Domains WHERE domain=domain
	IF title = '' BEGIN
		IF (SELECT COUNT(*) FROM Articles WHERE domainId=domainId) >= 10 BEGIN
			--get common word found in all article titles
			INSERT INTO title_results
			EXEC Domain_FindTitle domainId=domainId
		END
	END
	IF parentId > 0 AND parentId <> domainId BEGIN
		EXEC DomainLink_Add domainId=parentId, linkId=domainId
	END
END ELSE BEGIN
	--create domain ID
	INSERT INTO domain_results
	EXEC Domain_Add domain=domain, parentId=parentId
	SELECT domainId = domainId, title = title FROM Domains WHERE domain=domain
END
	IF NOT EXISTS(SELECT * FROM DownloadQueue WHERE url=url) 
	AND NOT EXISTS(SELECT * FROM Downloads WHERE url=url) BEGIN
		SET qid = nextval('public."SequenceDownloadQueue"')
		INSERT INTO DownloadQueue (qid, "url", "path", feedId, domainId, "status", datecreated) 
		VALUES (qid, url, public.GetPathFromUrl(url, domain), feedId, domainId, 0, CURRENT_TIMESTAMP)
		UPDATE Domains SET inqueue+=1 WHERE domainId=domainId
	END ELSE BEGIN
		SELECT qid = qid FROM DownloadQueue WHERE url=url
		IF qid IS NULL BEGIN
			SELECT qid = id FROM Downloads WHERE url=url
		END
	END
	SELECT qid AS qid
END;

$$;
