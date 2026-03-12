CREATE OR REPLACE PROCEDURE  public."Domain_AnalyzerRule_Remove"
(
    IN ruleId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM AnalyzerRules WHERE ruleId=ruleId
END;

$$;
