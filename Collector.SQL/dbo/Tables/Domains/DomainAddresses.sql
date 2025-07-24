CREATE TABLE [dbo].[DomainAddresses]
(
	[addressId] INT NOT NULL PRIMARY KEY,
	[address] NVARCHAR(64) NOT NULL DEFAULT '',
    [city] NVARCHAR(32) NOT NULL DEFAULT '',
    [state] VARCHAR(3) NOT NULL DEFAULT '',
    [zipcode] VARCHAR(12) NOT NULL DEFAULT '',
)