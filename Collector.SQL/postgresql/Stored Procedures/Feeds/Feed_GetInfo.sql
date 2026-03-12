CREATE OR REPLACE PROCEDURE  public."Feed_GetInfo"
(
    IN feedId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM Feeds WHERE feedId=feedId
END;

$$;
