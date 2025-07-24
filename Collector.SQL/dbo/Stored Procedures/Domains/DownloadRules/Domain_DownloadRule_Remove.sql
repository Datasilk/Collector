CREATE PROCEDURE [dbo].[Domain_DownloadRule_Remove]
	@ruleId int
AS
	DELETE FROM DownloadRules WHERE ruleId=@ruleId