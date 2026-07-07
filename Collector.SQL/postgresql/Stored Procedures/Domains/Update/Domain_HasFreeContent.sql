CREATE OR REPLACE FUNCTION public."Domain_HasFreeContent"
(
    p_domainId INT,
    p_free BOOLEAN DEFAULT FALSE
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."Domains" SET "free" = p_free, "dateupdated" = CURRENT_TIMESTAMP WHERE "domainId" = p_domainId;
END;
$$;