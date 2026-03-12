CREATE OR REPLACE PROCEDURE public."Downloads_GetCount"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT COUNT(*) FROM Downloads
END;

$$;
