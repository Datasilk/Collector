CREATE OR REPLACE FUNCTION public."Domain_Collections_GetList"()
RETURNS TABLE(
    "colId" INT, "colgroupId" INT, "name" VARCHAR(32), "search" VARCHAR(128),
    "subjectId" INT, "filtertype" INT, "type" INT, "sort" INT, "lang" VARCHAR(6), "datecreated" TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT c.* FROM public."DomainCollections" c
    LEFT JOIN public."DomainCollectionGroups" g ON g."colgroupId" = c."colgroupId"
    ORDER BY g."name" ASC, c."name" ASC;
END;
$$;