CREATE OR REPLACE PROCEDURE  public."Domain_DownloadRule_Remove"
(
    IN ruleId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM DownloadRules WHERE ruleId=ruleId
END;

$$;
