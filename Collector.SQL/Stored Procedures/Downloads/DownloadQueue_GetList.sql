DROP FUNCTION IF EXISTS public."DownloadQueue_GetList"(TEXT, INT, TEXT, INT, INT);

CREATE OR REPLACE FUNCTION public."DownloadQueue_GetList"(
    p_search TEXT DEFAULT '',
    p_status INT DEFAULT -1,
    p_sort TEXT DEFAULT 'datecreated DESC',
    p_start INT DEFAULT 1,
    p_length INT DEFAULT 100
)
RETURNS TABLE(
    qid BIGINT,
    "feedId" INT,
    "domainId" INT,
    type SMALLINT,
    status INT,
    tries INT,
    url VARCHAR(255),
    path VARCHAR(255),
    datecreated TIMESTAMP(6)
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_order TEXT;
BEGIN
    v_order := LOWER(p_sort);
    IF v_order NOT IN ('datecreated asc', 'datecreated desc', 'url asc', 'url desc', 'status asc', 'status desc') THEN
        v_order := 'datecreated DESC';
    END IF;

    RETURN QUERY EXECUTE format(
        'SELECT qid::BIGINT, "feedId"::INT, "domainId"::INT, type::SMALLINT, status::INT, tries::INT, url::VARCHAR(255), path::VARCHAR(255), datecreated::TIMESTAMP(6) FROM public."DownloadQueue" WHERE 1 = 1%s%s ORDER BY %s LIMIT %s OFFSET %s',
        CASE WHEN p_search IS NOT NULL AND p_search <> '' THEN format(' AND url ILIKE %s', quote_literal('%' || p_search || '%')) ELSE '' END,
        CASE WHEN p_status >= 0 THEN format(' AND status = %s', p_status) ELSE '' END,
        v_order,
        p_length,
        p_start - 1
    );
END;
$$;
