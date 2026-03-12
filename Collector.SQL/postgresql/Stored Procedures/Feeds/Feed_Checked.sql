CREATE OR REPLACE PROCEDURE  public."Feed_Checked"
(
    IN feedId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Feeds SET lastChecked=CURRENT_TIMESTAMP WHERE feedId=feedId
RETURN 0
END;

$$;
