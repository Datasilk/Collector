CREATE OR REPLACE PROCEDURE  public."Domain_DownloadRules_GetForDomains"
(
    IN domains TEXT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * INTO #domain_names FROM public.SplitArray(domains, ',')
	SELECT r.*, d.domain FROM "Domains" d
	JOIN DownloadRules r ON r.domainId = d.domainId
	WHERE d.domain IN (SELECT value FROM #domain_names)
	ORDER BY d.domainId ASC
END;

$$;
