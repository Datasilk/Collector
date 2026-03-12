CREATE TABLE IF NOT EXISTS public."ArticleBugs"
(
    "bugId" INT NOT NULL PRIMARY KEY,
    "articleId" INT NULL,
    "title" VARCHAR(100) NULL,
    "description" TEXT NULL,
    "datecreated" TIMESTAMP NULL,
    "status" SMALLINT NULL
);
