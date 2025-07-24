CREATE PROCEDURE [dbo].[Domain_UpdateType2]
	@domainId int = 0,
	@type int = -1
AS
	UPDATE Domains SET [type2] = @type, dateupdated = GETUTCDATE()
	WHERE domainId=@domainId