CREATE PROCEDURE [dbo].[DomainLinks_GetList]
	@domainId int
AS
	SELECT d.*,
	(CASE WHEN wl.domain IS NOT NULL THEN 1 ELSE 0 END) AS whitelisted,
	(CASE WHEN bl.domain IS NOT NULL THEN 1 ELSE 0 END) AS blacklisted
	FROM DomainLinks dl
	JOIN Domains d ON d.domainId = dl.linkId
	LEFT JOIN Whitelist_Domains wl ON wl.domain = d.domain
	LEFT JOIN Blacklist_Domains bl ON bl.domain = d.domain
	WHERE dl.domainId = @domainId
	ORDER BY whitelisted DESC, blacklisted, d.domain ASC
