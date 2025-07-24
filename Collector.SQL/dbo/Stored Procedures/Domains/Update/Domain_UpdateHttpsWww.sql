CREATE PROCEDURE [dbo].[Domain_UpdateHttpsWww]
	@domainId int = 0,
	@https bit = 0,
	@www bit = 0
AS
	UPDATE Domains SET [https] = @https, [www] = @www, dateupdated = GETUTCDATE()
	WHERE domainId=@domainId