CREATE OR REPLACE PROCEDURE  public."Article_Add"
(
    IN feedId INT DEFAULT 0,
    IN subjects INT DEFAULT 0,
    IN subjectId INT DEFAULT 0,
    IN score smallint DEFAULT 0,
    IN domain VARCHAR(50),
    IN url VARCHAR(250),
    IN title VARCHAR(250) DEFAULT '',
    IN summary VARCHAR(250) DEFAULT '',
    filesize DOUBLE PRECISION = 0,
    IN linkcount INT DEFAULT 0,
    IN linkwordcount INT DEFAULT 0,
    IN wordcount INT DEFAULT 0,
    IN sentencecount smallint DEFAULT 0,
    IN paragraphcount smallint DEFAULT 0,
    IN importantcount smallint DEFAULT 0,
    IN yearstart smallint DEFAULT 0,
    IN yearend smallint DEFAULT 0,
    IN years VARCHAR(50) DEFAULT '',
    IN images smallint DEFAULT 0,
    IN datepublished TIMESTAMP DEFAULT NULL,
    IN relavance smallint DEFAULT 1,
    IN importance smallint DEFAULT 1,
    IN fiction smallint DEFAULT 1,
    analyzed DOUBLE PRECISION = 0.1,
    IN active BOOLEAN DEFAULT TRUE
);
LANGUAGE plpgsql
AS $$
DECLARE
    articleId INT := NULL;
    domainId INT := NULL;
    domain_results TABLE (id INT);
BEGIN
SELECT domainId = domainId FROM Domains WHERE domain=domain
SELECT articleId = articleId FROM Articles WHERE url=url
IF domainId IS NULL BEGIN
	INSERT INTO domain_results
	EXEC Domain_Add domain=domain, parentId=0
	SELECT domainId = domainId FROM Domains WHERE domain=domain
END
IF articleId IS NULL BEGIN
	SET articleId = nextval('public."SequenceArticles"')
	INSERT INTO Articles 
	(articleId, feedId, subjects, subjectId, domainId, score, domain, url, title, summary, filesize, linkcount, linkwordcount, wordcount, sentencecount, paragraphcount, importantcount, analyzecount,
	yearstart, yearend, years, images, datecreated, datepublished, relavance, importance, fiction, analyzed, active)
	VALUES 
	(articleId, feedId, subjects, subjectId, domainId, score, domain, url, title, summary, filesize, linkcount, linkwordcount, wordcount, sentencecount, paragraphcount, importantcount, 1,
	yearstart, yearend, years, images, CURRENT_TIMESTAMP, datepublished, relavance, importance, fiction, analyzed, active)
	--update domain record
	UPDATE Domains SET articles+=1 WHERE domainId=domainId
END
--archive related Download Queue record
UPDATE DownloadQueue SET status=0 WHERE "url" = url
SELECT articleId
END;

$$;
