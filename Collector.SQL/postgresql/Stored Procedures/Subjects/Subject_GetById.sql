CREATE OR REPLACE FUNCTION public."Subject_GetById"
(
    p_subjectId INT
)
RETURNS TABLE("subjectId" INT, "parentId" INT, "grammartype" INT, "score" INT, "haswords" BOOLEAN, "title" VARCHAR(50), "hierarchy" VARCHAR(50), "breadcrumb" VARCHAR(500))
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT * FROM public."Subjects" s WHERE s."subjectId" = p_subjectId;
END;
$$;