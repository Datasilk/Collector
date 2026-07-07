CREATE OR REPLACE FUNCTION public."Feeds_GetListWithLogs"
(
    p_days INT DEFAULT 7,
    p_dateStart DATE
)
RETURNS TABLE(
    "feedId" INT,
    "title" VARCHAR(100),
    "url" VARCHAR(100),
    "checkIntervals" INT,
    "lastChecked" TIMESTAMP,
    "filter" TEXT,
    "loglinks" SMALLINT,
    "logdatechecked" TIMESTAMP
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT f."feedId", f."title", f."url", f."checkIntervals", f."lastChecked", f."filter",
           NULL::SMALLINT AS "loglinks", NULL::TIMESTAMP AS "logdatechecked"
    FROM public."Feeds" f
    WHERE f."feedId" > 0
    UNION ALL
    SELECT l."feedId", NULL::VARCHAR(100), NULL::VARCHAR(100), NULL::INT,
           NULL::TIMESTAMP, NULL::TEXT, l."links", l."datechecked"
    FROM public."FeedsCheckedLog" l
    WHERE l."datechecked" >= p_dateStart
    AND l."datechecked" <= p_dateStart + (p_days || ' days')::INTERVAL
    ORDER BY "feedId", "logdatechecked";
END;
$$;