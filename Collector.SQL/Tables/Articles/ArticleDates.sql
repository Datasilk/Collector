CREATE TABLE IF NOT EXISTS public."ArticleDates"
(
    "articleId" INT NOT NULL PRIMARY KEY,
    "date" DATE NULL,
    "hasyear" BOOLEAN NULL,
    "hasmonth" BOOLEAN NULL,
    "hasday" BOOLEAN NULL
);
