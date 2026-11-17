DROP FUNCTION IF EXISTS public."Downloads_GetFilteredCount"(TEXT, INT, INT);

CREATE OR REPLACE FUNCTION public."Downloads_GetFilteredCount"(
    p_search TEXT DEFAULT '',
    p_status INT DEFAULT -1,
    p_type INT DEFAULT -1
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_count INT;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM public."Downloads" d
    WHERE (p_search IS NULL OR p_search = '' OR d."url" ILIKE '%' || p_search || '%')
      AND (p_status < 0 OR d."status" = p_status)
      AND (p_type < 0 OR d."type" = p_type);

    RETURN v_count;
END;
$$;
