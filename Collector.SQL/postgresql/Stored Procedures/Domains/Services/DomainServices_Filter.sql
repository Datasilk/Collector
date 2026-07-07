CREATE OR REPLACE FUNCTION public."DomainServices_Filter"
(
    p_search VARCHAR(100) DEFAULT NULL,
    p_start INT DEFAULT 0,
    p_length INT DEFAULT 50
)
RETURNS TABLE("Id" INT, "Name" VARCHAR(100))
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT dsn."Id", dsn."Name"
    FROM public."DomainServiceNames" dsn
    WHERE (p_search IS NULL OR dsn."Name" ILIKE '%' || p_search || '%')
    ORDER BY dsn."Name" ASC
    OFFSET p_start ROWS
    FETCH NEXT p_length ROWS ONLY;
END;
$$;