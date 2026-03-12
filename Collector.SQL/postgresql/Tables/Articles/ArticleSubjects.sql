CREATE TABLE IF NOT EXISTS public."ArticleSubjects"
(
    "subjectId" INT NOT NULL,
    "articleId" INT NULL,
    "score" SMALLINT NULL,
    "datecreated" TIMESTAMP NULL,
    "datepublished" TIMESTAMP NULL
);
