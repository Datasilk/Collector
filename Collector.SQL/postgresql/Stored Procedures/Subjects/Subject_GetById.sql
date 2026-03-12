CREATE OR REPLACE PROCEDURE  public."Subject_GetById"
(
    IN subjectId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM Subjects WHERE subjectId=subjectId
END;

$$;
