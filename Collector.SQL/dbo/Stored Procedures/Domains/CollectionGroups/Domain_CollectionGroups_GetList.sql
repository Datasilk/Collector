CREATE PROCEDURE [dbo].[Domain_CollectionGroups_GetList]
AS
	SELECT * FROM DomainCollectionGroups ORDER BY [name] ASC