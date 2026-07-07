CREATE OR REPLACE FUNCTION public."Subject_GetByTitle"
(
    p_title VARCHAR(50),
    p_breadcrumb TEXT
)
RETURNS TABLE("subjectId" INT, "parentId" INT, "grammartype" INT, "score" INT, "haswords" BOOLEAN, "title" VARCHAR(50), "hierarchy" VARCHAR(50), "breadcrumb" VARCHAR(500))
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT * FROM public."Subjects" s WHERE s."breadcrumb" = p_breadcrumb AND s."title" = p_title;
END;
$$;