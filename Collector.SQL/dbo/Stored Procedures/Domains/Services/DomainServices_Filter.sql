CREATE PROCEDURE [dbo].[DomainServices_Filter]
    @search NVARCHAR(100) = NULL,
    @start INT = 0,
    @length INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get the total count for pagination
    DECLARE @totalCount INT;
    
    SELECT @totalCount = COUNT(*)
    FROM [dbo].[DomainServiceNames]
    WHERE (@search IS NULL OR Name LIKE '%' + @search + '%');
    
    -- Get the paginated results
    SELECT *
    FROM [dbo].[DomainServiceNames]
    WHERE (@search IS NULL OR Name LIKE '%' + @search + '%')
    ORDER BY Name ASC
    OFFSET @start ROWS
    FETCH NEXT @length ROWS ONLY;
    
    -- Return the total count as a second result set
    SELECT @totalCount AS TotalCount;
END
