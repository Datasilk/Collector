CREATE PROCEDURE [dbo].[Domains_GetCount]
	@subjectIds nvarchar(MAX) = '',
	@lang varchar(6) = '',
	@search nvarchar(MAX) = '',
	@type int = 0, -- 0 = all, 1 = whitelisted, 2 = blacklisted, 3 = not-listed, 4 = paywall, 5 = free, 6 = unprocessed, 7 = empty
	@domainType int = -1, 
	@domainType2 int = -1,
	@sort int = 0, -- 0 = ASC, 1 = DESC, 2 = most articles, 3 = newest, 4 = oldest, 5 = last updated
	@parentId int = -1
AS
	/* get subjects from array */
	SELECT * INTO #subjects FROM dbo.SplitArray(@subjectIds, ',')

	/* get domains that match a search term */
	SELECT * INTO #search FROM dbo.SplitArray(@search, ',')

	DECLARE @haswildcard bit = 0
	IF CHARINDEX('%', @search) > 0 SET @haswildcard = 1

	
	IF @type = 2 BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Blacklist table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT COUNT(*)
		FROM [Blacklist_Domains] d
		WHERE
		(
			(@search IS NOT NULL AND @search  <> '' AND (
				d.domain LIKE CASE WHEN @haswildcard = 1 THEN @search ELSE '%' + @search + '%' END
			))
			OR (@search IS NULL OR @search = '')
		)
	
	END ELSE IF @type = 8 BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Blacklist Wildcards table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT COUNT(*)
		FROM [Blacklist_Wildcards] d
		WHERE
		(
			(@search IS NOT NULL AND @search  <> '' AND (
				d.domain LIKE CASE WHEN @haswildcard = 1 THEN @search ELSE '%' + @search + '%' END
			))
			OR (@search IS NULL OR @search = '')
		)
		
	END ELSE BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Domains table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT COUNT(*)
		FROM [Domains] d
		LEFT JOIN Whitelist_Domains wl ON wl.domain = d.domain
		LEFT JOIN Blacklist_Domains bl ON bl.domain = d.domain
		WHERE
		(
			(@search IS NOT NULL AND @search  <> '' AND (
				d.title LIKE CASE WHEN @haswildcard = 1 THEN @search ELSE '%' + @search + '%' END
				OR d.domain LIKE CASE WHEN @haswildcard = 1 THEN @search ELSE '%' + @search + '%' END
			))
			OR (@search IS NULL OR @search = '')
		) AND (
			(@type = 0)
			OR (@type = 1 AND wl.domain IS NOT NULL)
			OR (@type = 2 AND bl.domain IS NOT NULL)
			OR (@type = 3 AND wl.domain IS NULL AND bl.domain IS NULL)
			OR (@type = 4 AND d.paywall = 1)
			OR (@type = 5 AND d.free = 1)
			OR (@type = 6 AND d.free = 0 AND d.paywall = 0 AND d.type = -1 AND bl.domain IS NULL AND wl.domain IS NULL)
			OR (@type = 7 AND d.[empty] = 1)
			OR (@type = 9 AND d.[empty] = 0)
		)
		AND (
			(@sort = 2 AND d.articles > 0)
			OR (@sort <> 2)
		)
		AND (
				(@domainType >= 0 AND @domainType2 < 0 AND (d.[type] = @domainType OR d.[type2] = @domainType))
				OR 
				(@domainType < 0 AND @domainType2 >= 0 AND (d.[type] = @domainType2 OR d.[type2] = @domainType2))
				OR 
				(@domainType >= 0 AND @domainType2 >= 0 AND (d.[type] = @domainType OR d.[type2] = @domainType
														  OR d.[type] = @domainType2 OR d.[type2] = @domainType2))
				OR 
				(@domainType < 0)
			)
		AND (
			(@parentId >= 0 AND d.parentId = @parentId)
			OR (@parentId < 0)
		)
		AND (
			(@lang != '' AND d.lang = @lang)
			OR @lang IS NULL OR @lang = ''
		)
		AND d.deleted = 0
	END
