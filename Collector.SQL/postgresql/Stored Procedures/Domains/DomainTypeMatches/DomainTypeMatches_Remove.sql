CREATE OR REPLACE PROCEDURE  public."DomainTypeMatches_Remove"
(
    IN matchId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM DomainTypeMatches WHERE matchId=matchId
END;

$$;
