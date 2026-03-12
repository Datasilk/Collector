CREATE TABLE IF NOT EXISTS public."DomainTypeMatches"
(
    "matchId" INT NOT NULL PRIMARY KEY,
    "type" INT NOT NULL DEFAULT -1,
    "type2" INT NOT NULL DEFAULT -1,
    "words" TEXT, --json serialized object
    "threshold" INT NOT NULL DEFAULT 1, -- minimum number of matches that must be found to succeed
    "rank" INT NOT NULL DEFAULT 0 -- when there are multiple matches, choose the lowest rank first
);
