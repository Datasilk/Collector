CREATE PROCEDURE [dbo].[Feed_GetInfo]
	@feedId int
AS
SELECT * FROM Feeds WHERE feedId=@feedId
