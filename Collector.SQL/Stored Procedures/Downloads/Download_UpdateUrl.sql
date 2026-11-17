CREATE OR REPLACE FUNCTION public."Download_UpdateUrl"
(
    p_qId BIGINT DEFAULT 0,
    p_url VARCHAR(250) DEFAULT '',
    p_domain VARCHAR(250) DEFAULT ''
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_domainId INT;
BEGIN
    SELECT d."domainId" INTO v_domainId FROM public."Domains" d WHERE d."domain" = p_domain;

    IF v_domainId IS NULL THEN
        v_domainId := nextval('public."SequenceDomains"');
        INSERT INTO public."Domains" ("domainId", "domain") VALUES (v_domainId, p_domain);
    END IF;

    DELETE FROM public."DownloadQueue" WHERE "url" = p_url;
    UPDATE public."DownloadQueue" SET "url" = p_url, "domainId" = v_domainId WHERE "qid" = p_qId;
    DELETE FROM public."Downloads" WHERE "url" = p_url;
    UPDATE public."Downloads" SET "url" = p_url, "domainId" = v_domainId WHERE "id" = p_qId;
END;
$$;