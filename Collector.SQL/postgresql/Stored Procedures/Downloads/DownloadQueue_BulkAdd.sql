CREATE OR REPLACE PROCEDURE  public."DownloadQueue_BulkAdd"
(
    IN urls TEXT DEFAULT '', --comma delimited list,
    IN domain VARCHAR(64) DEFAULT '',
    IN parentId INT,
    IN feedId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    cursor CURSOR, url TEXT, domainId INT, qid BIGINT, count INT := 0, title VARCHAR(128);
    title_results TABLE (title TEXT);
    domain_results TABLE (id INT);
BEGIN
SELECT * INTO #urls FROM public.SplitArray(urls, ',')
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
SET cursor = CURSOR FOR
SELECT DISTINCT "value" FROM #urls
OPEN cursor
FETCH NEXT FROM cursor INTO url
WHILE @@FETCH_STATUS = 0 BEGIN
	IF NOT EXISTS(SELECT * FROM DownloadQueue WHERE url=url) 
	AND NOT EXISTS(SELECT * FROM Downloads WHERE url=url)
	AND NOT EXISTS(SELECT * FROM Articles WHERE url=url) BEGIN
		SET qid = nextval('public."SequenceDownloadQueue"')
		INSERT INTO DownloadQueue (qid, "url", "path", feedId, domainId, "status", datecreated) 
		VALUES (qid, url, public.GetPathFromUrl(url, domain), feedId, domainId, 0, CURRENT_TIMESTAMP)
		SET count = count + 1
	END
	FETCH NEXT FROM cursor INTO url
END
CLOSE cursor
DEALLOCATE cursor
UPDATE Domains SET inqueue+=count WHERE domainId=domainId
SELECT count
END;

$$;
