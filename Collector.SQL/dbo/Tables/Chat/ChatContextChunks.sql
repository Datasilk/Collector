CREATE TABLE [dbo].[ChatContextChunks]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [AppUserId] UNIQUEIDENTIFIER NOT NULL,
    [ChatId] UNIQUEIDENTIFIER NOT NULL,
    [Content] NVARCHAR(MAX) NOT NULL,
    [Embedding] VECTOR(768) NOT NULL,
    [Metadata] NVARCHAR(MAX) NULL,
    [Created] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_ChatContextChunks_AppUsers] FOREIGN KEY ([AppUserId]) REFERENCES [dbo].[AppUsers]([Id]),
    CONSTRAINT [FK_ChatContextChunks_Chats] FOREIGN KEY ([ChatId]) REFERENCES [dbo].[Chats]([Id])
)
