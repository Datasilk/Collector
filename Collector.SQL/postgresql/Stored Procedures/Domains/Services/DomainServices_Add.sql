CREATE OR REPLACE FUNCTION public."DomainServices_Add"
(
    p_domainId INT,
    p_serviceIds TEXT
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_serviceIds INT[];
BEGIN
    v_serviceIds := string_to_array(p_serviceIds, ',')::INT[];

    INSERT INTO public."DomainServices" ("domainId", "serviceId")
    SELECT p_domainId, si
    FROM unnest(v_serviceIds) AS si
    LEFT JOIN public."DomainServices" ds ON ds."domainId" = p_domainId AND ds."serviceId" = si
    WHERE ds."domainId" IS NULL;
END;
$$;