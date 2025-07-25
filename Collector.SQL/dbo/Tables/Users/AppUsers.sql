CREATE TABLE [dbo].[AppUsers]
(
	[Id] [UNIQUEIDENTIFIER] PRIMARY KEY CLUSTERED,
    [Email] [nvarchar](64) NOT NULL,
    [EmailConfirmed] [bit] NOT NULL,
    [FullName]  [nvarchar](64) NOT NULL DEFAULT '',
    [PasswordHash] [nvarchar](max) NULL,
    [LockoutEndDate] [datetime] NULL,
    [LockoutEnabled] [bit] NOT NULL,
    [AccessFailedCount] [int] NOT NULL DEFAULT 0,
	[AccessFailedTime] DateTime2 NULL,
    [PasswordResetHash] [nvarchar](128) NULL,
	[PasswordResetTime] DateTime2 NULL,
    [NewEmail] [nvarchar](64) NULL,
	[OneTimeLoginToken] [nvarchar](128) NULL,
	[OneTimeLoginExpiry] DateTime NULL,
	[Status] [int] NOT NULL DEFAULT 0,
    [Created] DateTime2 DEFAULT GETUTCDATE(),
)
