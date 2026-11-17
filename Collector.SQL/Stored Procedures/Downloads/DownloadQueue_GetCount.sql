DROP FUNCTION IF EXISTS public."DownloadQueue_GetCount"(TEXT, INT);

CREATE OR REPLACE FUNCTION public."DownloadQueue_GetCount"(
    p_search TEXT DEFAULT '',
    p_status INT DEFAULT -1
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_count INT;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM public."DownloadQueue" q
    WHERE (p_search IS NULL OR p_search = '' OR q."url" ILIKE '%' || p_search || '%')
      AND (p_status < 0 OR q."status" = p_status);

    RETURN v_count;
END;
$$;
