CREATE OR REPLACE FUNCTION public."Domain_UpdateInfo"
(
    p_domainId INT DEFAULT 0,
    p_title VARCHAR(128) DEFAULT '',
    p_description VARCHAR(255) DEFAULT '',
    p_lang CHAR(2) DEFAULT 'en'
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."Domains" SET "title" = p_title, "description" = p_description, "lang" = p_lang, "hastitle" = TRUE, "dateupdated" = CURRENT_TIMESTAMP
    WHERE "domainId" = p_domainId;
END;
$$;