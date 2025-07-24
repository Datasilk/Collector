CREATE PROCEDURE [dbo].[Blacklist_Domains_GetList]
AS
	SELECT domain FROM Blacklist_Domains ORDER BY domain ASC