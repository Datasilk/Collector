CREATE OR REPLACE PROCEDURE  public."FeedCheckedLog_Add"
(
    IN feedId INT DEFAULT 0,
    IN links INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
INSERT INTO FeedsCheckedLog (feedId, links, datechecked)
	VALUES (feedId, links, CURRENT_TIMESTAMP)
	UPDATE Feeds SET lastChecked = CURRENT_TIMESTAMP
END;

$$;
