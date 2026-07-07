CREATE OR REPLACE FUNCTION public."Domain_IsEmpty"
(
    p_domainId INT,
    p_empty BOOLEAN DEFAULT FALSE
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."Domains" SET "empty" = p_empty, "dateupdated" = CURRENT_TIMESTAMP WHERE "domainId" = p_domainId;
END;
$$;