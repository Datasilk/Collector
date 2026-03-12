CREATE TABLE IF NOT EXISTS public."DownloadQueue"
(
    "qid" BIGINT NOT NULL,
    "feedId" INT NULL,
    "domainId" INT NULL,
    "type" SMALLINT NULL,
    "status" INT NOT NULL DEFAULT 0,
    "tries" INT NOT NULL DEFAULT 0,
    "url" VARCHAR(255) NOT NULL,
    "path" VARCHAR(255) NOT NULL,
    "datecreated" TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP
);
ALTER TABLE public."DownloadQueue"
    ADD CONSTRAINT "PK_DownloadQueue" PRIMARY KEY ("qid");
