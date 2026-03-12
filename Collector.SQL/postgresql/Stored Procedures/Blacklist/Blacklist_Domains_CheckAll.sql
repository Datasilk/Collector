CREATE OR REPLACE PROCEDURE  public."Blacklist_Domains_CheckAll"
(
    IN domains TEXT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * INTO #domains FROM public.SplitArray(domains, ',')
	SELECT domain FROM Blacklist_Domains WHERE domain IN (SELECT "value" FROM #domains)
	DROP TABLE #domains
END;

$$;
