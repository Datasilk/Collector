CREATE OR REPLACE PROCEDURE public."Blacklist_Wildcards_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT domain FROM Blacklist_Wildcards ORDER BY domain ASC
END;

$$;
