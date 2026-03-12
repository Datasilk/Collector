CREATE OR REPLACE PROCEDURE  public."Articles_GetCount"
(
    IN subjectIds TEXT DEFAULT '',
    IN search TEXT DEFAULT '',
    IN feedId INT DEFAULT 0,
    IN domainId INT DEFAULT 0,
    IN score INT DEFAULT -9999,
    IN isActive INT DEFAULT 2,
    IN isDeleted BOOLEAN DEFAULT FALSE,
    IN minImages INT DEFAULT 0,
    IN dateStart TIMESTAMP(6) DEFAULT NULL,
    IN dateEnd TIMESTAMP(6) DEFAULT NULL,
    IN bugsonly BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
BEGIN
/* set default dates */
	IF (dateStart IS NULL) BEGIN SET dateStart = DATEADD(YEAR, -100, CURRENT_TIMESTAMP) END
	IF (dateEnd IS NULL) BEGIN SET dateEnd = DATEADD(YEAR, 100, CURRENT_TIMESTAMP) END
	PRINT FORMAT(dateStart, 'yyyy-MM-dd HH:mm:ss.fff')
	PRINT FORMAT(dateEnd, 'yyyy-MM-dd HH:mm:ss.fff')
	/* get subjects from array */
	SELECT * INTO #subjects FROM public.SplitArray(subjectIds, ',')
	SELECT articleId INTO #subjectarticles FROM ArticleSubjects
	WHERE subjectId IN (SELECT CONVERT(INT, value) FROM #subjects)
	AND datecreated >= CONVERT(TIMESTAMP, dateStart) AND datecreated <= CONVERT(TIMESTAMP, dateEnd)
	/* get articles that match a search term */
	SELECT * INTO #search FROM public.SplitArray(search, ',')
	SELECT wordid INTO #wordids FROM Words WHERE word IN (SELECT value FROM #search)
	SELECT articleId INTO #searchedarticles FROM ArticleWords
	WHERE wordId IN (SELECT * FROM #wordids)
	/* get list of articles that match filter */
	SELECT COUNT(*)
	FROM Articles a
	WHERE
	(
		a.articleId IN (SELECT * FROM #subjectarticles)
		OR a.articleId IN (SELECT * FROM #searchedarticles)
		OR (search IS NOT NULL AND search  <> '' AND (
			a.title LIKE '%' + search + '%'
			OR a.summary LIKE '%' + search + '%'
			OR a."url" LIKE '%' + search + '%'
		))
		OR (search IS NULL OR search = '')
	); 
	AND (
		(feedId > 0 AND a.feedId = feedId)
		OR feedId = 0
	);
	AND (
		(domainId > 0 AND a.domainId = domainId)
		OR domainId = 0
	);
	AND a.active = CASE WHEN isActive = 2 THEN a.active ELSE isActive END
	AND a.deleted = isDeleted
	AND a.score >= score
	AND (
		(minImages > 0 AND a.images >= minImages)
		OR minImages <= 0
	);
	AND a.datecreated >= dateStart AND a.datecreated <= dateEnd
END;

$$;
