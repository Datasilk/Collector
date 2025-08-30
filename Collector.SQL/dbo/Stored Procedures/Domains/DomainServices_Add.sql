CREATE PROCEDURE [dbo].[DomainServices_Add]
    @domainId INT,
    @serviceIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Create a temporary table to hold the service IDs
    CREATE TABLE #ServiceIds (
        [Id] INT
    );
    
    -- Insert the service IDs into the temporary table
    INSERT INTO #ServiceIds ([Id])
    SELECT CAST(value AS INT) FROM STRING_SPLIT(@serviceIds, ',');
    
    -- Insert new domain-service relationships (avoiding duplicates)
    INSERT INTO [dbo].[DomainServices] ([domainId], [serviceId])
    SELECT @domainId, si.[Id]
    FROM #ServiceIds si
    LEFT JOIN [dbo].[DomainServices] ds ON ds.[domainId] = @domainId AND ds.[serviceId] = si.[Id]
    WHERE ds.[domainId] IS NULL;
    
    -- Clean up
    DROP TABLE #ServiceIds;
END
