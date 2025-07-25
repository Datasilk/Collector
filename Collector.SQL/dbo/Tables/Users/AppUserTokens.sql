CREATE TABLE [dbo].[AppUserTokens]
(
	[Token] [nvarchar](255) NOT NULL PRIMARY KEY CLUSTERED,
	[AppUserId] [UNIQUEIDENTIFIER] NULL,
	[IsSpecialUser] bit NOT NULL DEFAULT 0,
	[SpecialUserName] [nvarchar](32) NULL,
	[Expiry] DateTime NOT NULL,
	[Created] DateTime NOT NULL DEFAULT GETUTCDATE(),
	[IPAddress] [nvarchar](50) NULL,
	[Revoked] DateTime NULL,
	[ReplacedByToken] [nvarchar](128) NULL, 
    CONSTRAINT [FK_AppUserTokens_AppUsers] FOREIGN KEY ([AppUserId]) REFERENCES [AppUsers]([Id])
)
