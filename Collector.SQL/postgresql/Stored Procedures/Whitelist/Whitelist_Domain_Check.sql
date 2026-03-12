CREATE OR REPLACE PROCEDURE  public."Whitelist_Domain_Check"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT COUNT(*) FROM Whitelist_Domains WHERE domain=domain
END;

$$;
