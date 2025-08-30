CREATE PROCEDURE [dbo].[DomainServices_GetByNames]
    @serviceNames NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Create a temporary table to hold the service names
    CREATE TABLE #ServiceNames (
        [Name] NVARCHAR(64)
    );
    
    -- Insert the service names into the temporary table
    INSERT INTO #ServiceNames ([Name])
    SELECT value FROM STRING_SPLIT(@serviceNames, ',');
    
    -- Create any service names that don't exist yet
    INSERT INTO [dbo].[DomainServiceNames] ([Name])
    SELECT DISTINCT sn.[Name]
    FROM #ServiceNames sn
    LEFT JOIN [dbo].[DomainServiceNames] dsn ON dsn.[Name] = sn.[Name]
    WHERE dsn.[Id] IS NULL;
    
    -- Return the IDs for all service names
    SELECT dsn.[Id], dsn.[Name]
    FROM [dbo].[DomainServiceNames] dsn
    INNER JOIN #ServiceNames sn ON dsn.[Name] = sn.[Name];
    
    -- Clean up
    DROP TABLE #ServiceNames;
END
