CREATE OR REPLACE PROCEDURE  public."Domain_CollectionGroup_Add"
(
    IN name VARCHAR(32)
);
LANGUAGE plpgsql
AS $$
DECLARE
    id INT := nextval('public."SequenceDomainCollectionGroups"');
BEGIN
INSERT INTO DomainCollectionGroups (colgroupId, "name")
	VALUES (id, name)
	SELECT id
END;

$$;
