CREATE OR REPLACE FUNCTION public."Domain_DownloadRule_Remove"
(
    p_ruleId INT
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public."DownloadRules" WHERE "ruleId" = p_ruleId;
END;
$$;