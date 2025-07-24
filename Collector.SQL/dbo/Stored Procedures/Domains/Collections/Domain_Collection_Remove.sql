CREATE PROCEDURE [dbo].[Domain_Collection_Remove]
	@colId int = 0
AS
	DELETE FROM DomainCollections WHERE colId=@colId