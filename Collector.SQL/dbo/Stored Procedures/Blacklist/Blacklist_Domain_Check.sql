CREATE PROCEDURE [dbo].[Blacklist_Domain_Check]
	@domain nvarchar(64)
AS
	SELECT COUNT(*) FROM Blacklist_Domains WHERE domain=@domain