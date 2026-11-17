CREATE TABLE IF NOT EXISTS public."JournalCategories"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalCategories"')),
    "AppUserId" UUID NOT NULL,
    "Title" VARCHAR(64) NOT NULL,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1,
    "Color" VARCHAR(16) NOT NULL --hex color (no #)
);
