CREATE PROCEDURE [dbo].[Words_GetList]
	@words nvarchar(MAX)
AS
SELECT * INTO #words FROM dbo.SplitArray(@words, ',')
SELECT w.* FROM Words w
WHERE word IN (SELECT value FROM #words)
