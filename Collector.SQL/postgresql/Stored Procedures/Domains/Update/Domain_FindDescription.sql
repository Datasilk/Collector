CREATE OR REPLACE FUNCTION public."Domain_FindDescription"
(
    p_domainId INT DEFAULT 0
)
RETURNS VARCHAR(255)
LANGUAGE plpgsql
AS $$
DECLARE
    v_description VARCHAR(255);
BEGIN
    SELECT a."summary" INTO v_description
    FROM public."Articles" a
    WHERE a."domainId" = p_domainId
    AND a."summary" <> ''
    ORDER BY LENGTH(a."url") ASC
    LIMIT 1;

    UPDATE public."Domains"
    SET "description" = v_description, "dateupdated" = CURRENT_TIMESTAMP
    WHERE "domainId" = p_domainId;

    RETURN v_description;
END;
$$;