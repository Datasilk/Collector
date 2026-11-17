CREATE OR REPLACE FUNCTION public."Feed_GetInfo"
(
    p_feedId INT
)
RETURNS TABLE("feedId" INT, "domainId" INT, "doctype" INT, "categoryId" INT, "title" VARCHAR(100), "url" VARCHAR(100), "checkIntervals" INT, "lastChecked" TIMESTAMP, "filter" TEXT)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT * FROM public."Feeds" f WHERE f."feedId" = p_feedId;
END;
$$;