CREATE PROCEDURE [dbo].[Domain_GetById]
	@domainId int
AS
	SELECT d.*, (SELECT COUNT(*) FROM Articles a WHERE a.domainId = @domainId) AS articles,
	CASE WHEN EXISTS(SELECT * FROM Whitelist_Domains WHERE domain=d.domain) THEN 1 ELSE 0 END AS whitelisted,
	CASE WHEN EXISTS(SELECT * FROM Blacklist_Domains WHERE domain=d.domain) THEN 1 ELSE 0 END AS blacklisted
	FROM [Domains] d
	WHERE d.domainId=@domainId
