CREATE TABLE IF NOT EXISTS public."JournalCheckListItems"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalCheckListItems"')),
    "CheckListId" INT NOT NULL,
    "Checked" BOOLEAN NOT NULL DEFAULT FALSE,
    "Title" VARCHAR(255) NOT NULL,
    "Icon" INT NOT NULL DEFAULT 0,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1,
    "Sort" INT NOT NULL DEFAULT 1
);
