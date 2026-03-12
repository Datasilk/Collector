CREATE OR REPLACE PROCEDURE public."Whitelist_Domains_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT domain FROM Whitelist_Domains ORDER BY domain ASC
END;

$$;
