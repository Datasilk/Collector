CREATE PROCEDURE [dbo].[DomainServices_Filter]
    @search NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT *
    FROM [dbo].[DomainServiceNames]
    WHERE (@search IS NULL OR Name LIKE '%' + @search + '%')
    ORDER BY Name ASC;
END
