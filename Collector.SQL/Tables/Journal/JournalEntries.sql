CREATE TABLE IF NOT EXISTS public."JournalEntries"
(
    "Id" UUID NOT NULL PRIMARY KEY DEFAULT uuid_generate_v4(),
    "JournalId" INT NOT NULL,
    "ParentEntryId" UUID NULL,
    "ChapterId" INT NULL,
    "Title" VARCHAR(128) NOT NULL,
    "Description" VARCHAR(512) NOT NULL,
    "Url" VARCHAR(255) NULL,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Modified" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1, --0=deleted, 1=active, 2=published, 3=archived
    "Encrypted" BOOLEAN NOT NULL DEFAULT FALSE, --if encrypted, cannot be published
    "Thumbnail" VARCHAR(128) NULL, --image to use as thumbnail
    "ThumbnailModuleId" VARCHAR(64) NULL, --module id that the thumbnail came from
    "Favorite" BOOLEAN NOT NULL DEFAULT FALSE --if entry is favorited
);
