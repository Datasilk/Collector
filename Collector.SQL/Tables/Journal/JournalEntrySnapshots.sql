CREATE TABLE IF NOT EXISTS public."JournalEntrySnapshots"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalEntrySnapshots"')),
    "EntryId" UUID NOT NULL,
    "JournalId" INT NOT NULL,
    "ChapterId" INT NULL,
    "Title" VARCHAR(128) NOT NULL,
    "Description" VARCHAR(512) NOT NULL,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Modified" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedSnapshot" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1, --0=deleted, 1=active, 2=published, 3=archived
    "Encrypted" BOOLEAN NOT NULL DEFAULT FALSE, --if encrypted, cannot be published
    "Thumbnail" VARCHAR(128) NULL --image to use as thumbnail
);
