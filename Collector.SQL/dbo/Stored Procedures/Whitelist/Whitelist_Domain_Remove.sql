CREATE PROCEDURE [dbo].[Whitelist_Domain_Remove]
	@domain nvarchar(64)
AS
	DELETE FROM Whitelist_Domains WHERE domain=@domain