CREATE OR REPLACE FUNCTION public."DomainServices_GetByNames"
(
    p_serviceNames TEXT
)
RETURNS TABLE("Id" INT, "Name" VARCHAR(64))
LANGUAGE plpgsql
AS $$
DECLARE
    v_serviceNames VARCHAR(64)[];
BEGIN
    v_serviceNames := string_to_array(p_serviceNames, ',');

    INSERT INTO public."DomainServiceNames" ("Name")
    SELECT DISTINCT sn
    FROM unnest(v_serviceNames) AS sn
    LEFT JOIN public."DomainServiceNames" dsn ON dsn."Name" = sn
    WHERE dsn."Id" IS NULL;

    RETURN QUERY
    SELECT dsn."Id", dsn."Name"
    FROM public."DomainServiceNames" dsn
    WHERE dsn."Name" = ANY(v_serviceNames);
END;
$$;