CREATE PROCEDURE [dbo].[Domain_Collection_Add]
	@colgroupId int = 0,
	@name nvarchar(32),
	@search nvarchar(128),
	@subjectId int = 0,
	@filtertype int = 0,
	@type int = 0,
	@sort int = 0,
	@lang varchar(6) = ''
AS
	DECLARE @id int = NEXT VALUE FOR SequenceDomainCollections
	INSERT INTO DomainCollections (colId, colgroupId, [name], [search], subjectId, filtertype, [type], [sort], lang, datecreated)
	VALUES (@id, @colgroupId, @name, @search, @subjectId, @filtertype, @type, @sort, @lang, GETUTCDATE())
	SELECT @id