CREATE PROCEDURE [dbo].[Whitelist_Domains_GetList]
AS
	SELECT domain FROM Whitelist_Domains ORDER BY domain ASC