CREATE OR REPLACE PROCEDURE  public."Blacklist_Domain_Remove"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM Blacklist_Domains WHERE domain=domain
END;

$$;
