CREATE PROCEDURE [dbo].[Domain_CleanDownloads]
	@domainId int
AS
	--get all article that will be deleted
	DECLARE @articles TABLE (articleId int)
	INSERT INTO @articles 
	SELECT DISTINCT a.articleId
	FROM Articles a
	JOIN DownloadRules r ON r.domainId = a.domainId
	WHERE a.domainId=@domainId 
	AND (
		(LEN(r.[url]) > 0 AND a.[url] LIKE '%' + r.[url] + '%')
		OR (LEN(r.title) > 0 AND a.title LIKE '%' + r.title + '%')
		OR (LEN(r.summary) > 0 AND a.summary LIKE '%' + r.summary + '%')
	)

	-- delete all associated articles
	DELETE FROM Articles WHERE articleId IN (SELECT articleId FROM @articles)
	DELETE FROM ArticleBugs WHERE articleId IN (SELECT articleId FROM @articles)
	DELETE FROM ArticleDates WHERE articleId IN (SELECT articleId FROM @articles)
	DELETE FROM ArticleSentences WHERE articleId IN (SELECT articleId FROM @articles)
	DELETE FROM ArticleSubjects WHERE articleId IN (SELECT articleId FROM @articles)
	DELETE FROM ArticleWords WHERE articleId IN (SELECT articleId FROM @articles)

	-- delete all associated download queue records
	DECLARE @a_total int, @b_total int
	DELETE FROM DownloadQueue WHERE qId IN (
		SELECT DISTINCT dq.qid FROM DownloadQueue dq
		JOIN DownloadRules r ON r.domainId = dq.domainId
		WHERE dq.domainId=@domainId 
		AND (
			(LEN(r.[url]) > 0 AND r.[rule]=0 AND dq.[url] LIKE '%' + r.[url] + '%')
		)
	)
	
	-- delete all associated download archive records
	DELETE FROM Downloads WHERE id IN (
		SELECT DISTINCT d.id FROM Downloads d
		JOIN DownloadRules r ON r.domainId = d.domainId
		WHERE d.domainId=@domainId 
		AND (
			(LEN(r.[url]) > 0 AND r.[rule]=0 AND d.[url] LIKE '%' + r.[url] + '%')
		)
	)
	
	