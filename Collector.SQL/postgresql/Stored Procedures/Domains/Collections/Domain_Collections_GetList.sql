CREATE OR REPLACE PROCEDURE public."Domain_Collections_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT c.* FROM DomainCollections c
	LEFT JOIN DomainCollectionGroups g ON g.colgroupId=c.colgroupId
	ORDER BY g."name" ASC, c."name" ASC
	SELECT * FROM DomainCollectionGroups
END;

$$;
