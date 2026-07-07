CREATE OR REPLACE FUNCTION public."DomainTypeMatches_Remove"
(
    p_matchId INT
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public."DomainTypeMatches" WHERE "matchId" = p_matchId;
END;
$$;