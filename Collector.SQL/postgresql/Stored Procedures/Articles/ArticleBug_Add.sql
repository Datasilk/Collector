CREATE OR REPLACE PROCEDURE  public."ArticleBug_Add"
(
    IN articleId INT DEFAULT 0,
    IN title VARCHAR(100) DEFAULT '',
    IN description TEXT DEFAULT '',
    IN status SMALLINT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    bugId INT := nextval('public."SequenceArticleBugs"');
BEGIN
INSERT INTO ArticleBugs (bugId, articleId, title, "description", datecreated, "status")
	VALUES (bugId, articleId, title, description, CURRENT_TIMESTAMP, status)
END;

$$;
