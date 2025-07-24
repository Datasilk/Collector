CREATE PROCEDURE [dbo].[Domain_IsEmpty]
	@domainId int,
	@empty bit = 0
AS
	UPDATE [Domains] SET [empty]=@empty, dateupdated = GETUTCDATE() WHERE domainId=@domainId
	
	