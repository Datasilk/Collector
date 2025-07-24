CREATE PROCEDURE [dbo].[Domain_HasFreeContent]
	@domainId int,
	@free bit = 0
AS
	UPDATE [Domains] SET free=@free, dateupdated = GETUTCDATE() WHERE domainId=@domainId
	
	