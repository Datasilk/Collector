CREATE OR REPLACE FUNCTION public."Domain_UpdateHttpsWww"
(
    p_domainId INT DEFAULT 0,
    p_https BOOLEAN DEFAULT FALSE,
    p_www BOOLEAN DEFAULT FALSE
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."Domains" SET "https" = p_https, "www" = p_www, "dateupdated" = CURRENT_TIMESTAMP
    WHERE "domainId" = p_domainId;
END;
$$;