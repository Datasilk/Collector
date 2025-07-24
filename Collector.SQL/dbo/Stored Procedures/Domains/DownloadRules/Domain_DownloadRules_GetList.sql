CREATE PROCEDURE [dbo].[Domain_DownloadRules_GetList]
	@domainId int
AS
	SELECT * FROM DownloadRules WHERE domainId=@domainId ORDER BY datecreated ASC
