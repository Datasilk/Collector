CREATE PROCEDURE [dbo].[DestroyCollection]
	@articles BIT = 1,
	@subjects BIT = 1,
	@topics BIT = 1
AS
	IF @articles = 1 OR @subjects = 1 BEGIN
		DELETE FROM ArticleBugs
		DELETE FROM ArticleDates
		DELETE FROM Articles
		DELETE FROM ArticleSentences
		DELETE FROM ArticleSubjects
		DELETE FROM ArticleWords
		DELETE FROM DownloadQueue
		DELETE FROM Downloads
		DELETE FROM FeedsCheckedLog
	END

	IF @subjects = 1 BEGIN
		DELETE FROM Subjects
		DELETE FROM Words
	END
