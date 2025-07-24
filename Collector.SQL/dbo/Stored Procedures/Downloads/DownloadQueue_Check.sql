CREATE PROCEDURE [dbo].[DownloadQueue_Check]
	@domaindelay int = 60, -- in seconds
	@domain nvarchar(64) = '',
	@feedId int = 0,
	@sort int = 0 -- 0 = newest, 1 = oldest, 2 = domain-level, 3 = random
AS
	DECLARE @qid bigint, @domainId int, @maxQid bigint, @randQid bigint

	--BEGIN TRANSACTION

	IF @sort = 3 BEGIN -- random queue item
		SELECT @maxQid = MAX(qid) FROM DownloadQueue
		SET @randQid = CONVERT(bigint, (RAND() * @maxQid))
	END

	IF @domain IS NOT NULL AND @domain <> '' BEGIN
		SELECT @domainId = domainId FROM Domains WHERE domain=@domain
	END

	DECLARE @checkedDomains TABLE (domainId int)

	INSERT INTO @checkedDomains
	SELECT domainId FROM Domains
	WHERE lastchecked >= DATEADD(SECOND, 0 - @domaindelay, GETUTCDATE())

	SELECT TOP 1 @qid = q.qid, @domainId = q.domainId
	FROM DownloadQueue q --WITH (TABLOCKX)
	JOIN Domains d ON d.domainId = q.domainId
	LEFT JOIN Whitelist_Domains w ON w.domain = d.domain -- must be a whitelisted domain
	LEFT JOIN Blacklist_Domains b ON b.domain = d.domain -- check for blacklisted domain
	WHERE
	(
		-- filter by domain name
		(@domain IS NOT NULL AND @domain <> '' AND q.domainId = @domainId)
		OR @domain IS NULL OR @domain = ''
	)

	-- filter domains that have not been checked recently
	AND q.domainId NOT IN (SELECT domainId FROM @checkedDomains)
	AND (
		-- filter by feed
		(@feedId > 0 AND q.feedId = @feedId)
		OR @feedId <= 0
	)
	-- filter domains that are not behind a paywall
	AND (d.paywall = 0 OR (d.paywall = 1 AND d.free = 1))
	AND ( 
		-- get random download queue item
		((@sort = 2 OR @sort = 3) AND @maxQid > 0 AND q.qid >= @randQid)
		OR @sort <> 3 OR @maxQid = 0
	) 
	AND (
		-- get download queue item that only contains domain name (domain home page)
		(@sort = 2 AND LEN(q.[url]) <= LEN(d.domain) + 11)
		OR @sort != 2
	)
	AND (
		-- filter by whitelisted domains only (unless we're getting domain home pages)
		(@sort != 2 AND w.domain IS NOT NULL)
		OR @sort = 2
	)
	-- filter all blacklisted domains
	AND b.domain IS NULL
	AND q.status = 0
	AND (d.lang = '' OR d.lang = 'en')

	ORDER BY 
	CASE WHEN @sort = 0 THEN q.datecreated END DESC,
	CASE WHEN @sort = 2 THEN LEN(q.url) END


	IF @qid > 0 BEGIN
		
		--WAITFOR DELAY '00:00:03' -- for debugging transactions

		UPDATE DownloadQueue SET status=1 WHERE qid=@qid
		UPDATE Domains SET lastchecked = GETUTCDATE()
		WHERE domainId = @domainId

		-- get next download in the queue
		SELECT q.*, d.domain, d.articles
		FROM DownloadQueue q 
		JOIN Domains d ON d.domainId = q.domainId
		WHERE qid=@qid

		-- get list of download rules for domain that queue item belongs to
		SELECT * FROM DownloadRules WHERE domainId = (SELECT domainId FROM DownloadQueue q WHERE qid=@qid)
	END
	
	--COMMIT