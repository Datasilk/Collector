CREATE PROCEDURE [dbo].[Articles_GetList]
	@subjectIds nvarchar(MAX) = '',
	@search nvarchar(MAX) = '',
	@feedId int = 0,
	@domainId int = 0,
	@score int = -9999,
	@isActive int = 2,
	@isDeleted bit = 0,
	@minImages int = 0,
	@dateStart datetime2(7) = NULL,
	@dateEnd datetime2(7) = NULL,
	@orderby int = 5,
	@start int = 1,
	@length int = 50,
	@bugsonly bit = 0
AS
	/* set default dates */
	IF (@dateStart IS NULL) BEGIN SET @dateStart = DATEADD(YEAR, -100, GETUTCDATE()) END
	IF (@dateEnd IS NULL) BEGIN SET @dateEnd = DATEADD(YEAR, 100, GETUTCDATE()) END
	PRINT FORMAT(@dateStart, 'yyyy-MM-dd HH:mm:ss.fff')
	PRINT FORMAT(@dateEnd, 'yyyy-MM-dd HH:mm:ss.fff')
	/* get subjects from array */
	SELECT * INTO #subjects FROM dbo.SplitArray(@subjectIds, ',')
	SELECT articleId INTO #subjectarticles FROM ArticleSubjects
	WHERE subjectId IN (SELECT CONVERT(int, value) FROM #subjects)
	AND datecreated >= CONVERT(datetime, @dateStart) AND datecreated <= CONVERT(datetime, @dateEnd)

	/* get articles that match a search term */
	SELECT * INTO #search FROM dbo.SplitArray(@search, ',')
	SELECT wordid INTO #wordids FROM Words WHERE word IN (SELECT value FROM #search)
	SELECT articleId INTO #searchedarticles FROM ArticleWords
	WHERE wordId IN (SELECT * FROM #wordids)

	/* get list of articles that match filter */
	SELECT * FROM (
		SELECT ROW_NUMBER() OVER(ORDER BY 
			CASE WHEN @orderby = 0 THEN a.title END ASC,
			CASE WHEN @orderby = 1 THEN a.title END DESC,
			CASE WHEN @orderby = 2 THEN a.[url] END ASC,
			CASE WHEN @orderby = 3 THEN a.[url] END DESC,
			CASE WHEN @orderby = 4 THEN a.score END ASC,
			CASE WHEN @orderby = 5 THEN a.score END DESC,
			CASE WHEN @orderby = 6 THEN a.datecreated END DESC,
			CASE WHEN @orderby = 7 THEN a.datecreated END,
			CASE WHEN @orderby = 8 THEN a.visited END DESC
		) AS rownum, a.*,
		s.breadcrumb, s.hierarchy, s.title AS subjectTitle
		FROM Articles a 
		LEFT JOIN Subjects s ON s.subjectId=a.subjectId
		WHERE
		(
			a.articleId IN (SELECT * FROM #subjectarticles)
			OR a.articleId IN (SELECT * FROM #searchedarticles)
			OR (@search IS NOT NULL AND @search  <> '' AND (
				a.title LIKE '%' + @search + '%'
				OR a.summary LIKE '%' + @search + '%'
				OR a.[url] LIKE '%' + @search + '%'
			))
			OR (@search IS NULL OR @search = '')
		) 
		AND (
			(@feedId > 0 AND a.feedId = @feedId)
			OR @feedId = 0
		)
		AND (
			(@domainId > 0 AND a.domainId = @domainId)
			OR @domainId = 0
		)
		AND a.active = CASE WHEN @isActive = 2 THEN a.active ELSE @isActive END
		AND a.deleted = @isDeleted
		AND a.score >= @score
		AND (
			(@minImages > 0 AND a.images >= @minImages)
			OR @minImages <= 0
		)
		AND a.datecreated >= @dateStart AND a.datecreated <= @dateEnd
	) AS tbl WHERE rownum >= @start AND rownum < @start + @length
