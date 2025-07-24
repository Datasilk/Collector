CREATE PROCEDURE [dbo].[Download_UpdateUrl]
	@qId bigint = 0,
	@url nvarchar(250),
	@domain nvarchar(250)
AS

	DECLARE @domainId int
	SELECT @domainId=domainId FROM Domains WHERE domain=@domain

	IF @domainId IS NULL BEGIN
		SET @domainId = NEXT VALUE FOR SequenceDomains
		INSERT INTO Domains (domainId, domain) VALUES (@domainId, @domain)
	END

	IF EXISTS(SELECT * FROM DownloadQueue WHERE url=@url) BEGIN
		--remove existing download queue item
		DELETE FROM DownloadQueue WHERE url=@url
	END
	UPDATE DownloadQueue SET [url]=@url, domainId=@domainId WHERE qid=@qid
	

	IF EXISTS(SELECT * FROM Downloads WHERE url=@url) BEGIN
		DELETE FROM Downloads WHERE url=@url
	END
	UPDATE Downloads SET [url]=@url, domainId=@domainId WHERE id=@qid
