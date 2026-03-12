CREATE OR REPLACE PROCEDURE  public."Domain_FindTitle"
(
    IN domainId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    articles TABLE (;
    words TABLE (;
    count INT := 0;
    exclude TABLE(value VARCHAR(32));
    cursor CURSOR, title VARCHAR(250);
    i INT := 0;
    domain VARCHAR(64), domainpart VARCHAR(64), domainpart2 VARCHAR(64);
    domainparts TABLE (value VARCHAR(64));
    domainTitle VARCHAR(128);
BEGIN
--get common word found in all article titles
		title VARCHAR(250)
	);
		word TEXT
	);
	INSERT INTO articles 
	SELECT top 100 a.title FROM Articles a
	WHERE a.domainId = domainId
	SELECT count = COUNT(*) FROM articles
	INSERT INTO exclude ("value")
	VALUES ('and'), ('or'), ('&'), ('the'), ('for'), ('with')
	SET cursor = CURSOR FOR
	SELECT title FROM articles
	OPEN cursor
	FETCH NEXT FROM cursor INTO title
	WHILE @@FETCH_STATUS = 0 BEGIN
		--get all words & phrases from the title
		INSERT INTO words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(title, ' ')) as tbl WHERE LEN(value) > 2 AND "value" NOT IN (SELECT * FROM exclude)
		INSERT INTO words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(title, '-')) as tbl WHERE LEN(value) > 2 AND "value" NOT IN (SELECT * FROM exclude)
		INSERT INTO words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(title, '|')) as tbl WHERE LEN(value) > 2 AND "value" NOT IN (SELECT * FROM exclude)
		INSERT INTO words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(title, ':')) as tbl WHERE LEN(value) > 2 AND "value" NOT IN (SELECT * FROM exclude)
		INSERT INTO words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(title, ';')) as tbl WHERE LEN(value) > 2 AND "value" NOT IN (SELECT * FROM exclude)
		INSERT INTO words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(title, '/')) as tbl WHERE LEN(value) > 2 AND "value" NOT IN (SELECT * FROM exclude)
		FETCH NEXT FROM cursor INTO title
	END
	CLOSE cursor
	DEALLOCATE cursor
	SELECT count = COUNT(*) FROM words
	--get count of all duplicate words & phrases
	SELECT domain = domain FROM Domains WHERE domainId=domainId
	INSERT INTO domainparts SELECT * FROM STRING_SPLIT(domain, '.')
	SELECT TOP 1 domainpart = REPLACE("value", '-', '%') FROM domainparts
	SELECT domainpart2 = STRING_AGG("value", '') FROM domainparts
	--PRINT domainpart2
	SELECT TOP 1 domainTitle = TRIM(word)
	FROM (
		SELECT b.score, w.word, COUNT(w.word) AS total, LEN(w.word) AS "length"
		FROM words w
		CROSS APPLY (
			SELECT CASE 
			WHEN PATINDEX(domainpart + '%', REPLACE(w.word, ' ', '')) > 0 THEN 50 
			WHEN PATINDEX(domainpart2 + '%', REPLACE(w.word, ' ', '')) > 0 THEN 100
			ELSE 0 END AS score
		) AS b
		GROUP BY w.word, b.score
		HAVING COUNT(w.word) > 1
	) AS tbl
	ORDER BY score DESC, total DESC, "length" DESC
	UPDATE Domains SET title=domainTitle, hastitle=1, dateupdated = CURRENT_TIMESTAMP WHERE domainId=domainId
	SELECT domainTitle
END;

$$;
