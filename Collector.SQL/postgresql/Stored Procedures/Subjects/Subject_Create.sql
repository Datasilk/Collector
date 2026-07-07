CREATE OR REPLACE FUNCTION public."Subject_Create"
(
    p_parentId INT DEFAULT 0,
    p_grammartype INT DEFAULT 0,
    p_score INT DEFAULT 0,
    p_title VARCHAR(50),
    p_breadcrumb TEXT DEFAULT ''
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_create BOOLEAN := TRUE;
    v_hierarchy VARCHAR(50) := '';
    v_id INT := nextval('public."SequenceSubjects"');
    v_count INT;
BEGIN
    IF p_parentId > 0 THEN
        SELECT COUNT(*) INTO v_count
        FROM public."Subjects"
        WHERE "breadcrumb" = p_breadcrumb AND "title" = p_title;

        IF v_count > 0 THEN
            v_create := FALSE;
        ELSE
            SELECT s."hierarchy" INTO v_hierarchy
            FROM public."Subjects" s
            WHERE s."subjectId" = p_parentId;

            IF v_hierarchy <> '' THEN
                v_hierarchy := v_hierarchy || '>' || p_parentId::VARCHAR;
            ELSE
                v_hierarchy := p_parentId::VARCHAR;
            END IF;
        END IF;
    ELSE
        SELECT COUNT(*) INTO v_count
        FROM public."Subjects"
        WHERE "parentId" = 0 AND "title" = p_title;

        IF v_count > 0 THEN
            v_create := FALSE;
        END IF;
    END IF;

    IF v_create THEN
        INSERT INTO public."Subjects" ("subjectId", "parentId", "grammartype", "score", "title", "breadcrumb", "hierarchy")
        VALUES (v_id, p_parentId, p_grammartype, p_score, p_title, p_breadcrumb, v_hierarchy);
        RETURN v_id;
    ELSE
        RETURN 0;
    END IF;
END;
$$;