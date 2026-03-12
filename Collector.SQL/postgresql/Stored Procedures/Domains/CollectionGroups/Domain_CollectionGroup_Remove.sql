CREATE OR REPLACE PROCEDURE  public."Domain_CollectionGroup_Remove"
(
    IN colgroupId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM DomainCollectionGroups WHERE colgroupId=colgroupId
END;

$$;
