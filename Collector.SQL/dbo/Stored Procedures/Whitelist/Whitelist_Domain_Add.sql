CREATE PROCEDURE [dbo].[Whitelist_Domain_Add]
	@domain nvarchar(64)
AS
	DECLARE @domainId int
	BEGIN TRY
	INSERT INTO Whitelist_Domains (domain) VALUES (@domain)
	END TRY
	BEGIN CATCH
	END CATCH