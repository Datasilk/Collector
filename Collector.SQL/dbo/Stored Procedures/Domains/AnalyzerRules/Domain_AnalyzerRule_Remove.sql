CREATE PROCEDURE [dbo].[Domain_AnalyzerRule_Remove]
	@ruleId int
AS
	DELETE FROM AnalyzerRules WHERE ruleId=@ruleId