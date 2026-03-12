CREATE TABLE IF NOT EXISTS public."Subjects"
(
    "subjectId" INT NOT NULL PRIMARY KEY,
    "parentId" INT NULL DEFAULT 0,
    "grammartype" INT NULL DEFAULT 0,
    "score" INT NULL DEFAULT 0,
    "haswords" BOOLEAN NULL DEFAULT FALSE,
    "title" VARCHAR(50) NULL DEFAULT '',
    "hierarchy" VARCHAR(50) NULL DEFAULT '',
    "breadcrumb" VARCHAR(500) NULL DEFAULT ''
);
