CREATE OR REPLACE PROCEDURE  public."DomainServices_Add"
(
    IN domainId INT,
    IN serviceIds TEXT
);
LANGUAGE plpgsql
AS $$
BEGIN
    -- Create a temporary table to hold the service IDs
    CREATE TABLE IF NOT EXISTS #ServiceIds
(
    "Id" INT
);
    -- Insert the service IDs into the temporary table
    INSERT INTO #ServiceIds ("Id")
    SELECT CAST(value AS INT) FROM STRING_SPLIT(serviceIds, ',');
    -- Insert new domain-service relationships (avoiding duplicates)
    INSERT INTO public."DomainServices" ("domainId", "serviceId")
    SELECT domainId, si."Id"
    FROM #ServiceIds si
    LEFT JOIN public."DomainServices" ds ON ds."domainId" = domainId AND ds."serviceId" = si."Id"
    WHERE ds."domainId" IS NULL;
    -- Clean up
    DROP TABLE #ServiceIds;
END

$$;
