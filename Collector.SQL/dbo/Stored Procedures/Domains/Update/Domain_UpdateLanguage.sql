CREATE PROCEDURE [dbo].[Domain_UpdateLanguage]
	@domainId int = 0,
	@lang varchar(6)
AS
	UPDATE Domains SET lang = @lang, dateupdated = GETUTCDATE()
	WHERE domainId=@domainId