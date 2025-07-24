CREATE PROCEDURE [dbo].[Subject_GetByTitle]
	@title nvarchar(50),
	@breadcrumb nvarchar(MAX)
AS
SELECT * FROM Subjects WHERE breadcrumb = @breadcrumb AND title=@title
