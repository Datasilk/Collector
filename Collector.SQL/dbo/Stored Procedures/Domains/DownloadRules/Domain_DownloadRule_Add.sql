CREATE PROCEDURE [dbo].[Domain_DownloadRule_Add]
	@domainId int,
	@rule bit = 0,
	@url varchar(64) = '',
	@title varchar(64) = '',
	@summary varchar(64) = ''
AS
	DECLARE @id int = NEXT VALUE FOR SequenceDownloadRules
	INSERT INTO DownloadRules (ruleId, domainId, [rule], [url], title, summary, datecreated)
	VALUES (@id, @domainId, @rule, @url, @title, @summary, GETUTCDATE())

	SELECT @id