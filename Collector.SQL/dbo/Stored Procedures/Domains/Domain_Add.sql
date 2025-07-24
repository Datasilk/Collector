CREATE PROCEDURE [dbo].[Domain_Add]
	@domain nvarchar(64),
	@title nvarchar(128) = '',
	@parentId int = 0,
	@type int = 0 -- 0 = none, 1 = whitelist, 2 = blacklist
AS
	DECLARE @id int = NEXT VALUE FOR SequenceDomains
	INSERT INTO Domains (domainId, parentId, domain, title, lastchecked)
	VALUES (@id, @parentId, @domain, @title, DATEADD(HOUR, -1, GETUTCDATE()))
	SELECT @id

	IF @parentId > 0 BEGIN
		BEGIN TRY
			INSERT INTO DomainHierarchy (domainId, parentId, [level])
			SELECT @id, parentId, [level]
			FROM DomainHierarchy WHERE domainId = @parentId
		END TRY BEGIN CATCH END CATCH
		DECLARE @level int
		SELECT @level = ISNULL(MAX([level]), 0) + 1 FROM DomainHierarchy WHERE domainId = @parentId
		
		BEGIN TRY
			INSERT INTO DomainHierarchy (domainId, parentId, [level])
			VALUES (@id, @parentId, @level)
		END TRY BEGIN CATCH END CATCH

		EXEC DomainLink_Add @domainId=@parentId, @linkId=@id
	END

	IF @type = 1 EXEC Whitelist_Domain_Add @domain=@domain
	IF @type = 2 EXEC Blacklist_Domain_Add @domain=@domain

	DECLARE @url nvarchar(MAX) = 'http://' + @domain
	EXEC DownloadQueue_Add @url=@url, @domain=@domain, @parentId=@parentId, @feedId=0