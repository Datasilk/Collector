CREATE OR REPLACE PROCEDURE  public."ArticleSubjects_Remove"
(
    IN articleId INT DEFAULT 0,
    IN subjectId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
IF subjectId = 0 BEGIN
		DELETE FROM ArticleSubjects WHERE articleId=articleId
	END ELSE BEGIN
		DELETE FROM ArticleSubjects WHERE articleId=articleId AND subjectId=subjectId
	END
RETURN 0
END;

$$;
