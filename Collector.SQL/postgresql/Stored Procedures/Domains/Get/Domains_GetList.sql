CREATE OR REPLACE PROCEDURE  public."Domains_GetList"
(
    IN subjectIds TEXT DEFAULT '',
    IN lang varchar(6) DEFAULT '',
    IN search TEXT DEFAULT '',
    IN type INT DEFAULT 0, -- 0 = all, 1 = whitelisted, 2 = blacklisted, 3 = not-listed, 4 = paywall, 5 = free, 6 = unprocessed, 7 = empty,
    IN domainType INT DEFAULT -1,
    IN domainType2 INT DEFAULT -1,
    IN sort INT DEFAULT 0, -- 0 = Domain ASC, 1 = Domain DESC, 2 = Articles DESC, 3 = DateCreated DESC, 4 = DateCreated ASC, 5 = DateUpdated DESC,
    IN start INT DEFAULT 1,
    IN length INT DEFAULT 50,
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
	--PRINT 'has wildcard = ' + CONVERT(VARCHAR(1), haswildcard)
	IF type = 2 BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Blacklist table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT * FROM (
			SELECT ROW_NUMBER() OVER(ORDER BY 
			CASE WHEN sort = 0 OR sort = 6 THEN d.domain END,
			CASE WHEN sort = 1 OR sort = 7 THEN d.domain END DESC
			) AS rownum, d.domain, -1 AS "type"
			FROM "Blacklist_Domains" d
			WHERE
			(
				(search IS NOT NULL AND search  <> '' AND (
					d.domain LIKE CASE WHEN haswildcard = 1 THEN search ELSE '%' + search + '%' END
				))
				OR (search IS NULL OR search = '')
			);
		) AS tbl WHERE rownum >= start AND rownum < start + length
	END ELSE IF type = 8 BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Blacklist Wildcards table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT * FROM (
			SELECT ROW_NUMBER() OVER(ORDER BY 
			CASE WHEN sort = 0 OR sort = 6 THEN d.domain END,
			CASE WHEN sort = 1 OR sort = 7 THEN d.domain END DESC
			) AS rownum, d.domain, -2 AS "type"
			FROM "Blacklist_Wildcards" d
			WHERE
			(
				(search IS NOT NULL AND search  <> '' AND (
					d.domain LIKE CASE WHEN haswildcard = 1 THEN search ELSE '%' + search + '%' END
				))
				OR (search IS NULL OR search = '')
			);
		) AS tbl WHERE rownum >= start AND rownum < start + length
	END ELSE BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Domains table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT CAST(value AS INT) AS serviceId 
		INTO #serviceIds
		FROM STRING_SPLIT(serviceIds, ',')
		SELECT * FROM (
			SELECT ROW_NUMBER() OVER(ORDER BY 
			-- Title sorting with hastitle priority
			CASE WHEN sort = 0 OR sort = 1 OR sort = 6 OR sort = 7 THEN d.hastitle END DESC, 
			CASE WHEN sort = 6 THEN d.title END,
			CASE WHEN sort = 7 THEN d.title END DESC,
			-- Domain sorting
			CASE WHEN sort = 0 THEN d.domain END,
			CASE WHEN sort = 1 THEN d.domain END DESC,
			-- Articles sorting
			CASE WHEN sort = 2 THEN d.articles END DESC,
			CASE WHEN sort = 8 THEN d.articles END ASC,
			-- Date created sorting
			CASE WHEN sort = 3 THEN d.datecreated END DESC,
			CASE WHEN sort = 4 THEN d.datecreated END ASC,
			-- Date updated sorting
			CASE WHEN sort = 5 THEN d.dateupdated END DESC,
			CASE WHEN sort = 9 THEN d.dateupdated END ASC,
			-- Status sorting (whitelisted/blacklisted status)
			CASE WHEN sort = 10 THEN 
				CASE 
					WHEN wl.domain IS NOT NULL THEN 1
					WHEN bl.domain IS NOT NULL THEN 2
					ELSE 3
				END
			END ASC,
			CASE WHEN sort = 11 THEN 
				CASE 
					WHEN wl.domain IS NOT NULL THEN 1
					WHEN bl.domain IS NOT NULL THEN 2
					ELSE 3
				END
			END DESC
			) AS rownum, d.*,
			(CASE WHEN wl.domain IS NOT NULL THEN 1 ELSE 0 END) AS whitelisted,
			(CASE WHEN bl.domain IS NOT NULL THEN 1 ELSE 0 END) AS blacklisted
			FROM "Domains" d
			LEFT JOIN Whitelist_Domains wl ON wl.domain = d.domain
			LEFT JOIN Blacklist_Domains bl ON bl.domain = d.domain
			LEFT JOIN DomainServices ds ON ds.domainId = d.domainId AND ds.serviceId IN (SELECT * FROM #serviceIds)
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
				OR ds.serviceId IS NOT NULL
			);
		) AS tbl WHERE rownum >= start AND rownum < start + length
	END
END;

$$;
