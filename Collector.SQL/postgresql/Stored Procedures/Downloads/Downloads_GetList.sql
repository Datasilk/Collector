DROP FUNCTION IF EXISTS public."Downloads_GetList"(TEXT, INT, INT, TEXT, INT, INT);

CREATE OR REPLACE FUNCTION public."Downloads_GetList"(
    p_search TEXT DEFAULT '',
    p_status INT DEFAULT -1,
    p_type INT DEFAULT -1,
    p_sort TEXT DEFAULT 'datecreated DESC',
    p_start INT DEFAULT 1,
    p_length INT DEFAULT 100
)
RETURNS TABLE(
    id BIGINT,
    "feedId" INT,
    "domainId" INT,
    type SMALLINT,
    status INT,
    tries INT,
    url VARCHAR(255),
    path VARCHAR(255),
    datecreated TIMESTAMP(6),
    datearchived TIMESTAMP(6)
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_sql TEXT;
    v_order TEXT;
BEGIN
    v_sql := 'SELECT "id"::BIGINT, "feedId"::INT, "domainId"::INT, "type"::SMALLINT, "status"::INT, "tries"::INT, "url"::VARCHAR(255), "path"::VARCHAR(255), "datecreated"::TIMESTAMP(6), "datearchived"::TIMESTAMP(6) FROM public."Downloads" WHERE 1 = 1';

    IF p_search IS NOT NULL AND p_search <> '' THEN
        v_sql := v_sql || ' AND url ILIKE ''%' || p_search || '%''';
    END IF;

    IF p_status >= 0 THEN
        v_sql := v_sql || ' AND status = ' || p_status;
    END IF;

    IF p_type >= 0 THEN
        v_sql := v_sql || ' AND type = ' || p_type;
    END IF;

    IF p_sort IS NOT NULL AND p_sort <> '' THEN
        v_order := LOWER(p_sort);
        IF v_order NOT IN ('datecreated asc', 'datecreated desc', 'url asc', 'url desc', 'status asc', 'status desc', 'type asc', 'type desc') THEN
            v_order := 'datecreated DESC';
        END IF;
        v_sql := v_sql || ' ORDER BY ' || v_order;
    ELSE
        v_sql := v_sql || ' ORDER BY datecreated DESC';
    END IF;

    v_sql := v_sql || ' LIMIT ' || p_length || ' OFFSET ' || (p_start - 1);

    RETURN QUERY EXECUTE v_sql;
END;
$$;
