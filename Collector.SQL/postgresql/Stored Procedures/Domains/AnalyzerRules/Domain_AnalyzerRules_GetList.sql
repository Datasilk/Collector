CREATE OR REPLACE PROCEDURE  public."Domain_AnalyzerRules_GetList"
(
    IN domainId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM AnalyzerRules WHERE domainId=domainId ORDER BY datecreated ASC
END;

$$;
