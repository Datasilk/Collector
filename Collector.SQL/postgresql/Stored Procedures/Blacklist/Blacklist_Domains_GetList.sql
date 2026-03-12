CREATE OR REPLACE PROCEDURE public."Blacklist_Domains_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT domain FROM Blacklist_Domains ORDER BY domain ASC
END;

$$;
