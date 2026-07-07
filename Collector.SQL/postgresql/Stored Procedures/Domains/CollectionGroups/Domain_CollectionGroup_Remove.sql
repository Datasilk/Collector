CREATE OR REPLACE FUNCTION public."Domain_CollectionGroup_Remove"
(
    p_colgroupId INT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public."DomainCollectionGroups" WHERE "colgroupId" = p_colgroupId;
END;
$$;