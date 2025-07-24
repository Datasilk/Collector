CREATE PROCEDURE [dbo].[Domain_CollectionGroup_Remove]
	@colgroupId int = 0
AS
	DELETE FROM DomainCollectionGroups WHERE colgroupId=@colgroupId