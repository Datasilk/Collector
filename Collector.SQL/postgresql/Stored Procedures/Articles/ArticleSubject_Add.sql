CREATE OR REPLACE PROCEDURE  public."ArticleSubject_Add"
(
    IN articleId INT DEFAULT 0,
    IN subjectId INT DEFAULT 0,
    IN datepublished TIMESTAMP DEFAULT null,
    IN score INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
IF (SELECT COUNT(*) FROM ArticleSubjects WHERE articleId=articleId AND subjectId=subjectId) = 0 BEGIN
		INSERT INTO ArticleSubjects (articleId, subjectId, datecreated, datepublished, score) 
		VALUES (articleId, subjectId, CURRENT_TIMESTAMP, datepublished, score)
	END
END;

$$;
