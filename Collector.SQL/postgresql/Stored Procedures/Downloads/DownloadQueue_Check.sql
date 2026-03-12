CREATE OR REPLACE PROCEDURE  public."DownloadQueue_Check"
(
    IN domaindelay INT DEFAULT 60, -- in seconds,
    IN domain VARCHAR(64) DEFAULT '',
    IN feedId INT DEFAULT 0,
    IN sort INT DEFAULT 0, -- 0 = newest, 1 = oldest, 2 = domain-level, 3 = random,
    IN qid bigint DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT, maxQid bigint := 0, randQid bigint;
    checkedDomains TABLE (domainId INT);
BEGIN
--BEGIN TRANSACTION
	IF qid = 0 BEGIN
		IF sort = 2 OR sort = 3 BEGIN -- random queue item
			SELECT maxQid = MAX(qid) FROM DownloadQueue
			SET randQid = CONVERT(bigint, (RAND() * maxQid))
		END
		IF domain IS NOT NULL AND domain <> '' BEGIN
			SELECT domainId = domainId FROM Domains WHERE domain=domain
		END
		INSERT INTO checkedDomains
		SELECT domainId FROM Domains
		WHERE lastchecked >= DATEADD(SECOND, 0 - domaindelay, CURRENT_TIMESTAMP)
		SELECT TOP 1 qid = q.qid, domainId = q.domainId
		FROM DownloadQueue q --
		JOIN Domains d ON d.domainId = q.domainId
		LEFT JOIN Whitelist_Domains w ON w.domain = d.domain -- must be a whitelisted domain
		LEFT JOIN Blacklist_Domains b ON b.domain = d.domain -- check for blacklisted domain
		WHERE
		(
			-- filter by domain name
			(domain IS NOT NULL AND domain <> '' AND q.domainId = domainId)
			OR domain IS NULL OR domain = ''
		);
		-- filter domains that have not been checked recently
		AND q.domainId NOT IN (SELECT domainId FROM checkedDomains)
		AND (
			-- filter by feed
			(feedId > 0 AND q.feedId = feedId)
			OR feedId <= 0
		);
		-- filter domains that are not behind a paywall
		AND (d.paywall = 0 OR (d.paywall = 1 AND d.free = 1))
		AND ( 
			-- get random download queue item
			((sort = 2 OR sort = 3) AND maxQid > 0 AND q.qid >= randQid)
			OR maxQid = 0
		); 
		AND (
			-- get download queue item that only contains domain name (domain home page)
			(sort = 2 AND LEN(q."url") <= LEN(d.domain) + 11)
			OR sort != 2
		);
		AND (
			-- filter by whitelisted domains only (unless we're getting domain home pages)
			(sort != 2 AND w.domain IS NOT NULL)
			OR sort = 2
		);
		-- filter all blacklisted domains
		AND b.domain IS NULL
		AND q.status = 0
		AND (d.lang = '' OR d.lang = 'en')
		ORDER BY 
		CASE WHEN sort = 0 THEN q.datecreated END DESC
	END ELSE BEGIN
		SELECT domainId = domainId FROM DownloadQueue WHERE qid = qid
		IF domainId IS NULL RETURN --exit sproc if we fail to get domainId
	END
	IF qid > 0 BEGIN
		--WAITFOR DELAY '00:00:03' -- for debugging transactions
		UPDATE DownloadQueue SET status=1 WHERE qid=qid
		UPDATE Domains SET lastchecked = CURRENT_TIMESTAMP
		WHERE domainId = domainId
		-- get next download in the queue
		SELECT q.*, d.domain, d.articles
		FROM DownloadQueue q 
		JOIN Domains d ON d.domainId = q.domainId
		WHERE qid=qid
		-- get list of download rules for domain that queue item belongs to
		SELECT * FROM DownloadRules WHERE domainId = (SELECT domainId FROM DownloadQueue q WHERE qid=qid)
	END
	--COMMIT
END;

$$;
