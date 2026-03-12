CREATE TABLE IF NOT EXISTS public."DomainAddresses"
(
    "addressId" INT NOT NULL PRIMARY KEY,
    "address" VARCHAR(64) NOT NULL DEFAULT '',
    "city" VARCHAR(32) NOT NULL DEFAULT '',
    "state" VARCHAR(3) NOT NULL DEFAULT '',
    "zipcode" VARCHAR(12) NOT NULL DEFAULT ''
);
