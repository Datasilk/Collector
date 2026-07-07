CREATE OR REPLACE FUNCTION public."Domain_UpdateType2"
(
    p_domainId INT DEFAULT 0,
    p_type INT DEFAULT -1
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."Domains" SET "type2" = p_type, "dateupdated" = CURRENT_TIMESTAMP
    WHERE "domainId" = p_domainId;
END;
$$;