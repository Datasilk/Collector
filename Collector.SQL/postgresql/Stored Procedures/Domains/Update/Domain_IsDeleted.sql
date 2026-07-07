CREATE OR REPLACE FUNCTION public."Domain_IsDeleted"
(
    p_domainId INT,
    p_delete BOOLEAN DEFAULT TRUE
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."Domains" SET "deleted" = p_delete, "dateupdated" = CURRENT_TIMESTAMP WHERE "domainId" = p_domainId;
END;
$$;