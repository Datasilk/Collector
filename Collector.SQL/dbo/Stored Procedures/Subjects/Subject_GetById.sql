CREATE PROCEDURE [dbo].[Subject_GetById]
	@subjectId int
AS
SELECT * FROM Subjects WHERE subjectId=@subjectId
