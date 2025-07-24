CREATE PROCEDURE [dbo].[Feed_Add]
	@doctype int = 1,
	@categoryId int,
	@title nvarchar(100) = '',
	@url nvarchar(100) = '',
	@domain nvarchar(64) = '',
	@filter nvarchar(MAX) = '',
	@checkIntervals int = 720 --(12 hours)
AS
DECLARE @domainId INT
IF NOT EXISTS(SELECT * FROM Domains WHERE domain=@domain) BEGIN
	--get domain ID
	SELECT @domainId = domainId, @title = title FROM Domains WHERE domain=@domain
END ELSE BEGIN
	--create domain ID
	SET @domainId = NEXT VALUE FOR SequenceDomains
	INSERT INTO Domains (domainId, parentId, domain, lastchecked) VALUES (@domainId, 0, @domain, DATEADD(HOUR, -1, GETUTCDATE()))
END

DECLARE @feedId int = NEXT VALUE FOR SequenceFeeds
INSERT INTO Feeds (feedId, domainId, doctype, categoryId, title, url, checkIntervals, filter, lastChecked) 
VALUES (@feedId, @domainId, @doctype, @categoryId, @title, @url, @checkIntervals, @filter, DATEADD(HOUR, -24, GETUTCDATE()))
	
BEGIN TRY
	INSERT INTO Whitelist_Domains (domain) VALUES (@domain)
END TRY
BEGIN CATCH
END CATCH
	
SELECT @feedId

