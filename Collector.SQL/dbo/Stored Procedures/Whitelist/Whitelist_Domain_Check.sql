CREATE PROCEDURE [dbo].[Whitelist_Domain_Check]
	@domain nvarchar(64)
AS
	SELECT COUNT(*) FROM Whitelist_Domains WHERE domain=@domain