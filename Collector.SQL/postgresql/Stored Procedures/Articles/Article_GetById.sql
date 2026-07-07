CREATE OR REPLACE FUNCTION public."Article_GetById"
(
    p_articleId INT
)
RETURNS TABLE(
    "articleId" INT, "feedId" INT, "subjects" SMALLINT, "subjectId" INT, "domainId" INT,
    "score" SMALLINT, "images" SMALLINT, "filesize" DOUBLE PRECISION, "linkcount" INT, "linkwordcount" INT,
    "wordcount" INT, "sentencecount" SMALLINT, "paragraphcount" SMALLINT, "importantcount" SMALLINT,
    "analyzecount" SMALLINT, "yearstart" SMALLINT, "yearend" SMALLINT, "years" VARCHAR(50),
    "datecreated" TIMESTAMP, "datepublished" TIMESTAMP, "relavance" SMALLINT, "importance" SMALLINT,
    "fiction" SMALLINT, "domain" VARCHAR(50), "url" VARCHAR(250), "title" VARCHAR(250), "summary" VARCHAR(250),
    "analyzed" DOUBLE PRECISION, "visited" INT, "cached" BOOLEAN, "active" BOOLEAN, "deleted" BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT a.*, d."domain"
    FROM public."Articles" a
    LEFT JOIN public."Domains" d ON d."domainId" = a."domainId"
    WHERE a."articleId" = p_articleId;
END;
$$;