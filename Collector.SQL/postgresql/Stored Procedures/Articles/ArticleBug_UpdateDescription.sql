CREATE OR REPLACE PROCEDURE  public."ArticleBug_UpdateDescription"
(
    IN bugId INT DEFAULT 0,
    IN description TEXT DEFAULT ''
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE ArticleBugs SET description=description WHERE bugId=bugId
END;

$$;
