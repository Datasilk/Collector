CREATE PROCEDURE [dbo].[Domain_IsDeleted]
	@domainId int,
	@delete bit = 1
AS
	UPDATE [Domains] SET [deleted]=@delete, dateupdated = GETUTCDATE() WHERE domainId=@domainId
	
	