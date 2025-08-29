CREATE TABLE [dbo].[DownloadQueue]
(
	[qid] BIGINT NOT NULL,
    [feedId] INT NULL, 
    [domainId] INT NULL, 
    [type] TINYINT NULL, 
    [status] INT NOT NULL DEFAULT 0, 
    [tries] INT NOT NULL DEFAULT 0, 
	[url] NVARCHAR(255) NOT NULL,
    [path] NVARCHAR(255) NOT NULL,
    [datecreated] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE()
        
    CONSTRAINT [PK_DownloadQueue] PRIMARY KEY ([qid])
)

