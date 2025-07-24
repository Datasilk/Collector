CREATE PROCEDURE [dbo].[Domain_UpdateType]
	@domainId int = 0,
	@type int = -1
AS
	UPDATE Domains SET [type] = @type, dateupdated = GETUTCDATE()
	WHERE domainId=@domainId