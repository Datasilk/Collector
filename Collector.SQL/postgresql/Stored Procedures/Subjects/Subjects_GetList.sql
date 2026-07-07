CREATE OR REPLACE FUNCTION public."Subjects_GetList"
(
    p_subjectIds TEXT,
    p_parentId INT DEFAULT -1
)
RETURNS TABLE("subjectId" INT, "parentId" INT, "grammartype" INT, "score" INT, "haswords" BOOLEAN, "title" VARCHAR(50), "hierarchy" VARCHAR(50), "breadcrumb" VARCHAR(500))
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_subjectIds <> '' THEN
        RETURN QUERY
        SELECT * FROM public."Subjects" s
        WHERE s."subjectId" = ANY(string_to_array(p_subjectIds, ',')::INT[])
        AND s."parentId" = p_parentId
        ORDER BY s."title" ASC;
    ELSE
        RETURN QUERY
        SELECT * FROM public."Subjects" s
        WHERE s."parentId" = p_parentId
        ORDER BY s."title" ASC;
    END IF;
END;
$$;