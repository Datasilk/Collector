CREATE OR REPLACE PROCEDURE  public."Article_Clean"
(
    IN articleId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
EXEC ArticleSubjects_Remove articleId=articleId
	EXEC ArticleWords_Remove articleId=articleId
RETURN 0
END;

$$;
