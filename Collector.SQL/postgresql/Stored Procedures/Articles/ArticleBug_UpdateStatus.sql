CREATE OR REPLACE PROCEDURE  public."ArticleBug_UpdateStatus"
(
    IN bugId INT DEFAULT 0,
    IN status INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE ArticleBugs SET "status"=status WHERE bugId=bugId
END;

$$;
