CREATE TABLE IF NOT EXISTS public."StatisticsProjects"
(
    "projectId" INT NOT NULL PRIMARY KEY,
    "title" VARCHAR(100) NULL,
    "url" VARCHAR(100) NULL,
    "summary" VARCHAR(250) NULL,
    "publishdate" TIMESTAMP NULL
);
