CREATE OR REPLACE FUNCTION public."Domain_RequireSubscription"
(
    p_domainId INT,
    p_required BOOLEAN DEFAULT FALSE
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."Domains" SET "paywall" = p_required, "dateupdated" = CURRENT_TIMESTAMP WHERE "domainId" = p_domainId;
END;
$$;