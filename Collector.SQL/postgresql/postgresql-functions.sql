-- File: Functions/GetPathFromUrl.sql
CREATE OR REPLACE FUNCTION public."GetPathFromUrl"(
    url TEXT,
    domain VARCHAR(255)
)
RETURNS TEXT
LANGUAGE plpgsql
AS $$
DECLARE
    result TEXT;
    reverse_str TEXT;
    slash INT;
    domainPos INT;
BEGIN
    -- get path by removing all text to the left of the domain name (and removing the domain name too)
    domainPos := POSITION(domain IN url);
    IF domainPos = 0 THEN
        RETURN '';
    END IF;

    result := SUBSTRING(url FROM domainPos + LENGTH(domain));
    result := REPLACE(REPLACE(result, 'index.html', ''), 'index.php', '');

    -- remove query string
    IF POSITION('?' IN result) > 1 THEN
        result := SUBSTRING(result FROM 1 FOR POSITION('?' IN result) - 1);
    END IF;

    -- remove lingering slashes
    IF LENGTH(result) >= 1 AND LEFT(result, 1) = '/' THEN
        result := RIGHT(result, LENGTH(result) - 1);
    END IF;
    IF LENGTH(result) > 1 AND RIGHT(result, 1) = '/' THEN
        result := LEFT(result, LENGTH(result) - 1);
    END IF;

    -- check if file extension exists
    reverse_str := REVERSE(result);
    slash := POSITION('/' IN reverse_str);
    IF SUBSTRING(result FROM LENGTH(result) - 3 FOR 1) = '.' OR SUBSTRING(result FROM LENGTH(result) - 4 FOR 1) = '.' THEN
        -- reverse the result and find the last slash
        IF slash >= 1 THEN
            result := REVERSE(SUBSTRING(reverse_str FROM slash + 1 FOR LENGTH(reverse_str) - slash));
        ELSE
            result := '';
        END IF;
    END IF;

    -- finally, check if last item in path is too long to be part of path
    LOOP
        IF LENGTH(result) <= 1 THEN
            result := '';
            EXIT;
        END IF;
        reverse_str := REVERSE(result);
        slash := POSITION('/' IN reverse_str);
        IF slash >= 20 THEN
            result := REVERSE(SUBSTRING(reverse_str FROM slash + 1 FOR LENGTH(reverse_str) - slash));
        ELSE
            EXIT;
        END IF;
    END LOOP;

    RETURN result;
END;
$$;

-- File: Functions/ResetSequences.sql
CREATE OR REPLACE FUNCTION public."ResetSequences"()
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    rec RECORD;
    v_maxId BIGINT;
BEGIN
    -- Discover every sequence that is auto-attached to a table column (serial/bigserial)
    -- and reset it to MAX(column) + 1 so the next INSERT gets a fresh value.
    FOR rec IN
        SELECT
            quote_ident(n.nspname) || '.' || quote_ident(c.relname) AS sequence_name,
            quote_ident(tn.nspname) || '.' || quote_ident(t.relname) AS table_name,
            quote_ident(a.attname) AS column_name
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_depend d ON d.objid = c.oid
        JOIN pg_class t ON t.oid = d.refobjid
        JOIN pg_namespace tn ON tn.oid = t.relnamespace
        JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = d.refobjsubid
        WHERE c.relkind = 'S'
          AND d.deptype = 'a'
          AND n.nspname NOT IN ('pg_catalog', 'information_schema')
    LOOP
        EXECUTE format('SELECT COALESCE(MAX(%s), 0) FROM %s', rec.column_name, rec.table_name) INTO v_maxId;
        EXECUTE format('ALTER SEQUENCE %s RESTART WITH %s', rec.sequence_name, v_maxId + 1);
    END LOOP;
END;
$$;

-- File: Functions/SplitArray.sql
CREATE OR REPLACE FUNCTION public."SplitArray"(
    p_text TEXT DEFAULT '',
    p_delimiter VARCHAR(100) DEFAULT ','
)
RETURNS TABLE (
    "Position" INT,
    "valueInt" INT,
    "valueNum" NUMERIC(18,3),
    "value" VARCHAR(2000)
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_part TEXT;
    v_index INT := 0;
BEGIN
    IF p_text IS NULL OR TRIM(p_text) = '' THEN
        RETURN;
    END IF;

    FOR v_part IN SELECT TRIM(unnest(string_to_array(p_text, p_delimiter))) LOOP
        v_index := v_index + 1;
        "Position" := v_index;
        "value" := v_part;
        IF v_part ~ '^-?\d+(\.\d+)?$' THEN
            "valueInt" := v_part::INT;
            "valueNum" := v_part::NUMERIC(18,3);
        ELSE
            "valueInt" := NULL;
            "valueNum" := NULL;
        END IF;
        RETURN NEXT;
    END LOOP;
END;
$$;
