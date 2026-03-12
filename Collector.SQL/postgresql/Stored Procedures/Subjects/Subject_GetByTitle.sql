CREATE OR REPLACE PROCEDURE  public."Subject_GetByTitle"
(
    IN title VARCHAR(50),
    IN breadcrumb TEXT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM Subjects WHERE breadcrumb = breadcrumb AND title=title
END;

$$;
