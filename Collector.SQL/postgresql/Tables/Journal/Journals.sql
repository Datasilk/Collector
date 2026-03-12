CREATE TABLE IF NOT EXISTS public."Journals"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournals"')),
    "AppUserId" UUID NOT NULL,
    "CategoryId" INT NOT NULL,
    "Title" VARCHAR(64) NOT NULL,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1, --1 = active, 0 = archived, 8 = custom modules
    "Color" VARCHAR(16) NOT NULL, --hex color (no #)
    "EntryId" UUID NULL --default entry for the journal
);
