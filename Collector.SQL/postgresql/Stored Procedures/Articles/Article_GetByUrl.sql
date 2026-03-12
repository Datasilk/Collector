CREATE OR REPLACE PROCEDURE  public."Article_GetByUrl"
(
    IN url VARCHAR(250)
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM Articles WHERE url=url
RETURN 0
END;

$$;
