CREATE OR REPLACE PROCEDURE  public."DestroyCollection"
(
    IN articles BOOLEAN DEFAULT TRUE,
    IN subjects BOOLEAN DEFAULT TRUE,
    IN topics BOOLEAN DEFAULT TRUE
);
LANGUAGE plpgsql
AS $$
BEGIN
IF articles = 1 OR subjects = 1 BEGIN
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
	IF subjects = 1 BEGIN
		DELETE FROM Subjects
		DELETE FROM Words
	END
END;

$$;
