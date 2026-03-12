CREATE TABLE IF NOT EXISTS public."AppUsers"
(
    "Id" UUID PRIMARY KEY,
    "Email" VARCHAR(64) NOT NULL,
    "EmailConfirmed" BOOLEAN NOT NULL,
    "FullName" VARCHAR(64) NOT NULL DEFAULT '',
    "PasswordHash" TEXT NULL,
    "LockoutEndDate" TIMESTAMP NULL,
    "LockoutEnabled" BOOLEAN NOT NULL,
    "AccessFailedCount" INT NOT NULL DEFAULT 0,
    "AccessFailedTime" TIMESTAMPTZ NULL,
    "PasswordResetHash" VARCHAR(128) NULL,
    "PasswordResetTime" TIMESTAMPTZ NULL,
    "NewEmail" VARCHAR(64) NULL,
    "OneTimeLoginToken" VARCHAR(128) NULL,
    "OneTimeLoginExpiry" TIMESTAMP NULL,
    "EncryptionKey" VARCHAR(255) NULL,
    "EncryptionType" VARCHAR(16) NULL,
    "Status" INT NOT NULL DEFAULT 0,
    "Created" TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);
