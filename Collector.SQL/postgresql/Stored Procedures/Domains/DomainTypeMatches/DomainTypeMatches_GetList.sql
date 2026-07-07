CREATE OR REPLACE FUNCTION public."DomainTypeMatches_GetList"()
RETURNS TABLE("matchId" INT, "type" INT, "type2" INT, "words" TEXT, "threshold" INT, "rank" INT)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT * FROM public."DomainTypeMatches";
END;
$$;