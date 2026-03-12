CREATE TABLE IF NOT EXISTS public."JournalChapters"
(
    "ChapterId" INT NOT NULL,
    "JournalId" INT NOT NULL,
    "Title" VARCHAR(128) NOT NULL,
    "Sort" INT NOT NULL DEFAULT 1,
    "Icon" INT NOT NULL DEFAULT 0,
    "Color" INT NOT NULL DEFAULT 0,
    "Description" VARCHAR(256) NULL
);
