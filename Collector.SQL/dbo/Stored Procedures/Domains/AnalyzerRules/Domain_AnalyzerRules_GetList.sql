CREATE PROCEDURE [dbo].[Domain_AnalyzerRules_GetList]
	@domainId int
AS
	SELECT * FROM AnalyzerRules WHERE domainId=@domainId ORDER BY datecreated ASC
