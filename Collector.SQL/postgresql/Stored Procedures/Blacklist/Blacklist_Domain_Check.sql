CREATE OR REPLACE PROCEDURE  public."Blacklist_Domain_Check"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT COUNT(*) FROM Blacklist_Domains WHERE domain=domain
END;

$$;
