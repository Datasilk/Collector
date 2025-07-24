CREATE PROCEDURE [dbo].[Domain_DownloadRules_GetForDomains]
	@domains nvarchar(MAX)
AS
	SELECT * INTO #domain_names FROM dbo.SplitArray(@domains, ',')
	SELECT r.*, d.domain FROM [Domains] d
	JOIN DownloadRules r ON r.domainId = d.domainId
	WHERE d.domain IN (SELECT value FROM #domain_names)
	ORDER BY d.domainId ASC
