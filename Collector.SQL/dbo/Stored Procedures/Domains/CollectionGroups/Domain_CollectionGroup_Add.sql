CREATE PROCEDURE [dbo].[Domain_CollectionGroup_Add]
	@name nvarchar(32)
AS
	DECLARE @id int = NEXT VALUE FOR SequenceDomainCollectionGroups
	INSERT INTO DomainCollectionGroups (colgroupId, [name])
	VALUES (@id, @name)
	SELECT @id