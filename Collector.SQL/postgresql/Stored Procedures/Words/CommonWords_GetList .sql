CREATE OR REPLACE PROCEDURE public."CommonWords_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM CommonWords
END;

$$;
