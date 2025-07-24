CREATE PROCEDURE [dbo].[Article_Exists]
	@url nvarchar(250)
AS
	SELECT COUNT(*) FROM Articles WHERE url=@url
