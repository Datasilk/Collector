CREATE OR REPLACE PROCEDURE  public."Domains_GetCount"
(
    IN subjectIds TEXT DEFAULT '',
    IN lang varchar(6) DEFAULT '',
    IN search TEXT DEFAULT '',
    IN type INT DEFAULT 0, -- 0 = all, 1 = whitelisted, 2 = blacklisted, 3 = not-listed, 4 = paywall, 5 = free, 6 = unprocessed, 7 = empty,
    IN domainType INT DEFAULT -1,
    IN domainType2 INT DEFAULT -1,
    IN sort INT DEFAULT 0, -- 0 = ASC, 1 = DESC, 2 = most articles, 3 = newest, 4 = oldest, 5 = last updated,
    IN parentId INT DEFAULT -1,
    IN serviceIds TEXT DEFAULT NULL
);
LANGUAGE plpgsql
AS $$
DECLARE
    haswildcard BOOLEAN := 0;
BEGIN
/* get subjects from array */
	SELECT * INTO #subjects FROM public.SplitArray(subjectIds, ',')
	/* get domains that match a search term */
	SELECT * INTO #search FROM public.SplitArray(search, ',')
	IF CHARINDEX('%', search) > 0 SET haswildcard = 1
	IF type = 2 BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Blacklist table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT COUNT(*)
		FROM "Blacklist_Domains" d
		WHERE
		(
			(search IS NOT NULL AND search  <> '' AND (
				d.domain LIKE CASE WHEN haswildcard = 1 THEN search ELSE '%' + search + '%' END
			))
			OR (search IS NULL OR search = '')
		);
	END ELSE IF type = 8 BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Blacklist Wildcards table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT COUNT(*)
		FROM "Blacklist_Wildcards" d
		WHERE
		(
			(search IS NOT NULL AND search  <> '' AND (
				d.domain LIKE CASE WHEN haswildcard = 1 THEN search ELSE '%' + search + '%' END
			))
			OR (search IS NULL OR search = '')
		);
	END ELSE BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Domains table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT CAST(value AS INT) AS serviceId 
		INTO #serviceIds
		FROM STRING_SPLIT(serviceIds, ',')
		SELECT COUNT(*)
		FROM "Domains" d
		LEFT JOIN Whitelist_Domains wl ON wl.domain = d.domain
		LEFT JOIN Blacklist_Domains bl ON bl.domain = d.domain
		WHERE
		(
			(search IS NOT NULL AND search  <> '' AND (
				d.title LIKE CASE WHEN haswildcard = 1 THEN search ELSE '%' + search + '%' END
				OR d.domain LIKE CASE WHEN haswildcard = 1 THEN search ELSE '%' + search + '%' END
			))
			OR (search IS NULL OR search = '')
		) AND (
			(type = 0)
			OR (type = 1 AND wl.domain IS NOT NULL)
			OR (type = 2 AND bl.domain IS NOT NULL)
			OR (type = 3 AND wl.domain IS NULL AND bl.domain IS NULL)
			OR (type = 4 AND d.paywall = 1)
			OR (type = 5 AND d.free = 1)
			OR (type = 6 AND d.free = 0 AND d.paywall = 0 AND d.type = -1 AND bl.domain IS NULL AND wl.domain IS NULL)
			OR (type = 7 AND d."empty" = 1)
			OR (type = 9 AND d."empty" = 0)
		);
		AND (
			(sort = 2 AND d.articles > 0)
			OR (sort <> 2)
		);
		AND (
				(domainType >= 0 AND domainType2 < 0 AND (d."type" = domainType OR d."type2" = domainType))
				OR 
				(domainType < 0 AND domainType2 >= 0 AND (d."type" = domainType2 OR d."type2" = domainType2))
				OR 
				(domainType >= 0 AND domainType2 >= 0 AND (d."type" = domainType OR d."type2" = domainType
														  OR d."type" = domainType2 OR d."type2" = domainType2))
				OR 
				(domainType < 0)
			);
		AND (
			(parentId >= 0 AND d.parentId = parentId)
			OR (parentId < 0)
		);
		AND (
			(lang != '' AND d.lang = lang)
			OR lang IS NULL OR lang = ''
		);
		AND d.deleted = 0
		AND (
			serviceIds IS NULL
			OR EXISTS (
				SELECT 1 FROM public."DomainServices" ds
				INNER JOIN #serviceIds s ON ds.serviceId = s.serviceId
				WHERE ds.domainId = d.domainId
			);
		);
		DROP TABLE #serviceIds
	END
END;

$$;
