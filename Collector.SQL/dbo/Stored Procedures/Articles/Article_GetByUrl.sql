CREATE PROCEDURE [dbo].[Article_GetByUrl]
	@url nvarchar(250)
AS
	SELECT * FROM Articles WHERE url=@url
RETURN 0
