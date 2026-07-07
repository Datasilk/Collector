CREATE OR REPLACE FUNCTION public."Domain_CollectionGroup_Add"
(
    p_name VARCHAR(32)
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_id INT := nextval('public."SequenceDomainCollectionGroups"');
BEGIN
    INSERT INTO public."DomainCollectionGroups" ("colgroupId", "name")
    VALUES (v_id, p_name);

    RETURN v_id;
END;
$$;