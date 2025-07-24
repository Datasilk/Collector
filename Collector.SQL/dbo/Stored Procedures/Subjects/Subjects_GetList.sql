CREATE PROCEDURE [dbo].[Subjects_GetList]
	@subjectIds nvarchar(MAX),
	@parentId int = -1
AS
IF @subjectIds <> '' BEGIN
	SELECT * INTO #subjects FROM dbo.SplitArray(@subjectIds, ',')
	SELECT * FROM Subjects 
	WHERE subjectId IN (SELECT CONVERT(int, value) FROM #subjects)
	AND parentId = CASE WHEN @parentId >= 0 THEN @parentId ELSE parentId END
	ORDER BY title ASC
END ELSE BEGIN
/* parentId only */
	SELECT * FROM Subjects 
	WHERE parentId = CASE WHEN @parentId >= 0 THEN @parentId ELSE parentId END
	ORDER BY title ASC
END

