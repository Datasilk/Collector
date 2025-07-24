CREATE PROCEDURE [dbo].[Blacklist_Domain_Add]
	@domain nvarchar(64)
AS
	DECLARE @domainId int
	BEGIN TRY
	INSERT INTO Blacklist_Domains (domain) VALUES (@domain)
	END TRY
	BEGIN CATCH
	END CATCH
	SELECT @domainId=domainId FROM Domains WHERE domain=@domain

	-- delete all articles related to domain
	EXEC Domain_DeleteAllArticles @domainId=@domainId

	--delete all download queue related to domain
	DELETE FROM DownloadQueue WHERE domainId=@domainId
	DELETE FROM Downloads WHERE domainId=@domainId
	DELETE FROM Domains WHERE domainId=@domainId

	--delete whitelisted domains (if any)
	DELETE FROM Whitelist_Domains WHERE domain=@domain