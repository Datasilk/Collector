CREATE PROCEDURE [dbo].[Blacklist_Domain_Remove]
	@domain nvarchar(64)
AS
	DELETE FROM Blacklist_Domains WHERE domain=@domain