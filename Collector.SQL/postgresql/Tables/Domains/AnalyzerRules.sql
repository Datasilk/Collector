CREATE TABLE IF NOT EXISTS public."AnalyzerRules"
(
    "ruleId" INT NOT NULL PRIMARY KEY,
    "domainId" INT NOT NULL,
    "selector" VARCHAR(64) NOT NULL DEFAULT '',
    "rule" BOOLEAN NOT NULL DEFAULT FALSE, -- 0 = exclude, 1 = include
    "datecreated" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
