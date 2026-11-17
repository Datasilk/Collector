CREATE OR REPLACE FUNCTION public."Domain_AnalyzerRule_Remove"
(
    p_ruleId INT
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public."AnalyzerRules" WHERE "ruleId" = p_ruleId;
END;
$$;