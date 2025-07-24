CREATE PROCEDURE [dbo].[Blacklist_Wildcard_Add]
	@domain nvarchar(64)
AS
	INSERT INTO Blacklist_Wildcards (domain) VALUES (@domain)