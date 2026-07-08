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