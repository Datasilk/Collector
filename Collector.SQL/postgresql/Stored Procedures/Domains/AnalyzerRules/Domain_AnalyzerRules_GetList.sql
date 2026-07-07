CREATE OR REPLACE FUNCTION public."Domain_AnalyzerRules_GetList"
(
    p_domainId INT
)
RETURNS TABLE("ruleId" INT, "domainId" INT, "selector" VARCHAR(64), "rule" BOOLEAN, "datecreated" TIMESTAMPTZ)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT * FROM public."AnalyzerRules" a WHERE a."domainId" = p_domainId ORDER BY a."datecreated" ASC;
END;
$$;