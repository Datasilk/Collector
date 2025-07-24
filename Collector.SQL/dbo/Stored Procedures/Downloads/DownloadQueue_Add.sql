CREATE PROCEDURE [dbo].[DownloadQueue_Add]
	@url nvarchar(MAX) = '',
	@domain nvarchar(64) = '',
	@parentId int,
	@feedId int = 0
AS
DECLARE @domainId INT, @qid BIGINT, @count INT = 0, @title nvarchar(128)
IF EXISTS(SELECT * FROM Domains WHERE domain=@domain) BEGIN
	--get domain ID
	SELECT @domainId = domainId, @title = title FROM Domains WHERE domain=@domain
	IF @title = '' BEGIN
		IF (SELECT COUNT(*) FROM Articles WHERE domainId=@domainId) >= 10 BEGIN
			--get common word found in all article titles
			DECLARE @title_results TABLE (title nvarchar(MAX))
			INSERT INTO @title_results
			EXEC Domain_FindTitle @domainId=@domainId
		END
	END
	IF @parentId > 0 AND @parentId <> @domainId BEGIN
		EXEC DomainLink_Add @domainId=@parentId, @linkId=@domainId
	END

END ELSE BEGIN
	--create domain ID
	DECLARE @domain_results TABLE (id int)
	INSERT INTO @domain_results
	EXEC Domain_Add @domain=@domain, @parentId=@parentId
	SELECT @domainId = domainId, @title = title FROM Domains WHERE domain=@domain
END

	IF NOT EXISTS(SELECT * FROM DownloadQueue WHERE url=@url) 
	AND NOT EXISTS(SELECT * FROM Downloads WHERE url=@url) BEGIN
		SET @qid = NEXT VALUE FOR SequenceDownloadQueue
		INSERT INTO DownloadQueue (qid, [url], [path], feedId, domainId, [status], datecreated) 
		VALUES (@qid, @url, dbo.GetPathFromUrl(@url, @domain), @feedId, @domainId, 0, GETUTCDATE())
		UPDATE Domains SET inqueue+=1 WHERE domainId=@domainId
	END ELSE BEGIN
		SELECT @qid = qid FROM DownloadQueue WHERE url=@url
		IF @qid IS NULL BEGIN
			SELECT @qid = id FROM Downloads WHERE url=@url
		END
	END
	
	SELECT @qid AS qid


	