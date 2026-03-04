CREATE TABLE [dbo].[ChatHistory]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [ChatId] UNIQUEIDENTIFIER NOT NULL,
    [Role] INT NOT NULL, --user, assistant, system
    [Content] NVARCHAR(MAX) NOT NULL,
    [Model] NVARCHAR(50) NULL, --LLM model used (e.g., gpt-4, claude-3-opus, etc.)
    [Created] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Status] INT NOT NULL DEFAULT 1, --0=deleted, 1=active
    CONSTRAINT [FK_ChatHistory_Chats] FOREIGN KEY ([ChatId]) REFERENCES [dbo].[Chats]([Id])
)
