CREATE OR REPLACE FUNCTION public."Domain_UpdateLanguage"
(
    p_domainId INT DEFAULT 0,
    p_lang VARCHAR(6)
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."Domains" SET "lang" = p_lang, "dateupdated" = CURRENT_TIMESTAMP
    WHERE "domainId" = p_domainId;
END;
$$;