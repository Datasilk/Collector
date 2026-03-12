CREATE TABLE IF NOT EXISTS public."AppUserTokens"
(
    "Token" VARCHAR(255) NOT NULL PRIMARY KEY,
    "AppUserId" UUID NULL,
    "IsSpecialUser" BOOLEAN NOT NULL DEFAULT FALSE,
    "SpecialUserName" VARCHAR(32) NULL,
    "Expiry" TIMESTAMP NOT NULL,
    "Created" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IPAddress" VARCHAR(50) NULL,
    "Revoked" TIMESTAMP NULL,
    "ReplacedByToken" VARCHAR(128) NULL
);
ALTER TABLE public."AppUserTokens"
    ADD CONSTRAINT "FK_AppUserTokens_AppUsers" FOREIGN KEY ("AppUserId") REFERENCES public."AppUsers"("Id");
