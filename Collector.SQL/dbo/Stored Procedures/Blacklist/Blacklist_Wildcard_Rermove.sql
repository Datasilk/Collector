CREATE PROCEDURE [dbo].[Blacklist_Wildcard_Remove]
	@domain nvarchar(64)
AS
	DELETE FROM Blacklist_Wildcards WHERE domain=@domain