CREATE OR REPLACE PROCEDURE  public."DomainServices_GetByNames"
(
    IN serviceNames TEXT
);
LANGUAGE plpgsql
AS $$
BEGIN
    -- Create a temporary table to hold the service names
    CREATE TABLE IF NOT EXISTS #ServiceNames
(
    "Name" VARCHAR(64)
);
    -- Insert the service names into the temporary table
    INSERT INTO #ServiceNames ("Name")
    SELECT value FROM STRING_SPLIT(serviceNames, ',');
    -- Create any service names that don't exist yet
    INSERT INTO public."DomainServiceNames" ("Name")
    SELECT DISTINCT sn."Name"
    FROM #ServiceNames sn
    LEFT JOIN public."DomainServiceNames" dsn ON dsn."Name" = sn."Name"
    WHERE dsn."Id" IS NULL;
    -- Return the IDs for all service names
    SELECT dsn."Id", dsn."Name"
    FROM public."DomainServiceNames" dsn
    INNER JOIN #ServiceNames sn ON dsn."Name" = sn."Name";
    -- Clean up
    DROP TABLE #ServiceNames;
END

$$;
