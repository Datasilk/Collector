CREATE PROCEDURE [dbo].[Domain_RequireSubscription]
	@domainId int,
	@required bit = 0
AS
	UPDATE [Domains] SET paywall=@required, dateupdated = GETUTCDATE() WHERE domainId=@domainId
	
	