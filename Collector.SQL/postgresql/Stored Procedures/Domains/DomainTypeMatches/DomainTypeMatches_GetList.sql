CREATE OR REPLACE PROCEDURE public."DomainTypeMatches_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM DomainTypeMatches
END;

$$;
