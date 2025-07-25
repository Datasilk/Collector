CREATE TABLE [dbo].[AppUserRoles]
(
	[AppUserId] [UNIQUEIDENTIFIER] NOT NULL,
	[AppRoleId] INT NOT NULL,
	PRIMARY KEY (AppUserId, AppRoleId),
	CONSTRAINT FK_AppUserRoles_AppUserId FOREIGN KEY ([AppUserId]) REFERENCES AppUsers(Id),
	CONSTRAINT FK_AppUserRoles_AppRoleId FOREIGN KEY ([AppRoleId]) REFERENCES AppRoles(Id)
)
