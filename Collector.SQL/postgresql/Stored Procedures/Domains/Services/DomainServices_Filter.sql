CREATE OR REPLACE PROCEDURE  public."DomainServices_Filter"
(
    IN search VARCHAR(100) DEFAULT NULL,
    IN start INT DEFAULT 0,
    IN length INT DEFAULT 50
);
LANGUAGE plpgsql
AS $$
DECLARE
    totalCount INT;
BEGIN
    -- Get the total count for pagination
    SELECT COUNT(*)
FROM public."DomainServiceNames"
    WHERE (search IS NULL OR Name LIKE '%' + search + '%')
INTO totalCount;
    -- Get the paginated results
    SELECT *
    FROM public."DomainServiceNames"
    WHERE (search IS NULL OR Name LIKE '%' + search + '%')
    ORDER BY Name ASC
    OFFSET start ROWS
    FETCH NEXT length ROWS ONLY;
    -- Return the total count as a second result set
    SELECT totalCount AS TotalCount;
END

$$;
