CREATE PROCEDURE [dbo].[Blacklist_Domains_CheckAll]
	@domains nvarchar(MAX)
AS
	SELECT * INTO #domains FROM dbo.SplitArray(@domains, ',')
	SELECT domain FROM Blacklist_Domains WHERE domain IN (SELECT [value] FROM #domains)
	DROP TABLE #domains