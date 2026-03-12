CREATE OR REPLACE PROCEDURE  public."Article_Exists"
(
    IN url VARCHAR(250)
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT COUNT(*) FROM Articles WHERE url=url
END;

$$;
