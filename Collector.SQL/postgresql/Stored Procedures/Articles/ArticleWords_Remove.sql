CREATE OR REPLACE PROCEDURE  public."ArticleWords_Remove"
(
    IN articleId INT DEFAULT 0,
    IN word VARCHAR(50) DEFAULT ''
);
LANGUAGE plpgsql
AS $$
DECLARE
    wordId INT := 0;
BEGIN
IF word = '' BEGIN
		DELETE FROM ArticleWords WHERE articleId=articleId
	END ELSE BEGIN
		SELECT wordId=wordId FROM words WHERE word=word
		DELETE FROM ArticleWords WHERE articleId=articleId AND wordId=wordId
	END
RETURN 0
END;

$$;
