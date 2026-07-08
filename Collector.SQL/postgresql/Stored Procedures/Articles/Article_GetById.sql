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
    SELECT
        a."articleId", a."feedId", a."subjects", a."subjectId", a."domainId",
        a."score", a."images", a."filesize", a."linkcount", a."linkwordcount",
        a."wordcount", a."sentencecount", a."paragraphcount", a."importantcount",
        a."analyzecount", a."yearstart", a."yearend", a."years",
        a."datecreated", a."datepublished", a."relavance", a."importance",
        a."fiction", d."domain"::VARCHAR(50) AS "domain", a."url", a."title", a."summary",
        a."analyzed", a."visited", a."cached", a."active", a."deleted"
    FROM public."Articles" a
    LEFT JOIN public."Domains" d ON a."domainId" = d."domainId"
    WHERE a."articleId" = p_articleId;
END;
$$;