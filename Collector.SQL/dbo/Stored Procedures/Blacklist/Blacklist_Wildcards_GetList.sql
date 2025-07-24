CREATE PROCEDURE [dbo].[Blacklist_Wildcards_GetList]
AS
	SELECT domain FROM Blacklist_Wildcards ORDER BY domain ASC