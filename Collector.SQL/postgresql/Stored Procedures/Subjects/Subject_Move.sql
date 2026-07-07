CREATE OR REPLACE FUNCTION public."Subject_Move"
(
    p_subjectId INT DEFAULT 1,
    p_newParent INT DEFAULT 127
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_title VARCHAR(50) := '';
    v_bread VARCHAR(500) := '';
    v_hier VARCHAR(50);
    v_newBread VARCHAR(500) := '';
    v_newHier VARCHAR(50);
    v_newTitle VARCHAR(50);
    v_childId INT;
    v_parentId INT;
    v_parentTitle VARCHAR(50);
    v_parentHier VARCHAR(50);
    v_parentBread VARCHAR(500);
    rec RECORD;
BEGIN
    SELECT s."title", s."breadcrumb", s."hierarchy"
    INTO v_title, v_bread, v_hier
    FROM public."Subjects" s
    WHERE s."subjectId" = p_subjectId;

    IF v_bread <> '' THEN
        v_bread := v_bread || '>' || v_title;
        v_hier := v_hier || '>' || p_subjectId::VARCHAR;
    ELSE
        v_bread := v_title;
        v_hier := p_subjectId::VARCHAR;
    END IF;

    SELECT s."breadcrumb", s."hierarchy", s."title"
    INTO v_newBread, v_newHier, v_newTitle
    FROM public."Subjects" s
    WHERE s."subjectId" = p_newParent;

    IF v_newBread <> '' THEN
        v_newBread := v_newBread || '>' || v_newTitle;
        v_newHier := v_newHier || '>' || p_newParent::VARCHAR;
    ELSE
        v_newBread := v_newTitle;
        v_newHier := p_newParent::VARCHAR;
    END IF;

    UPDATE public."Subjects"
    SET "parentId" = p_newParent,
        "hierarchy" = v_newHier,
        "breadcrumb" = v_newBread
    WHERE "subjectId" = p_subjectId;

    FOR rec IN
        SELECT s."subjectId", s."parentId"
        FROM public."Subjects" s
        WHERE s."hierarchy" LIKE v_hier || '>%'
           OR s."hierarchy" = v_hier
        ORDER BY s."hierarchy" ASC
    LOOP
        v_childId := rec."subjectId";
        v_parentId := rec."parentId";

        SELECT s."title", s."hierarchy", s."breadcrumb"
        INTO v_parentTitle, v_parentHier, v_parentBread
        FROM public."Subjects" s
        WHERE s."subjectId" = v_parentId;

        IF v_parentBread <> '' THEN
            v_parentBread := v_parentBread || '>' || v_parentTitle;
            v_parentHier := v_parentHier || '>' || v_parentId::VARCHAR;
        ELSE
            v_parentBread := v_parentTitle;
            v_parentHier := v_parentId::VARCHAR;
        END IF;

        UPDATE public."Subjects"
        SET "hierarchy" = v_parentHier,
            "breadcrumb" = v_parentBread
        WHERE "subjectId" = v_childId;
    END LOOP;
END;
$$;