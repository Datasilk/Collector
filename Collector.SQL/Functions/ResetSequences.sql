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
