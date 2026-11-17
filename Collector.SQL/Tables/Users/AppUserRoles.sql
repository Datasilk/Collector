CREATE TABLE IF NOT EXISTS public."AppUserRoles"
(
    "AppUserId" UUID NOT NULL,
    "AppRoleId" INT NOT NULL,
    PRIMARY KEY ("AppUserId", "AppRoleId")
);
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conrelid = 'public."AppUserRoles"'::regclass AND conname = 'fk_appuserroles_appuserid'
    ) THEN
        ALTER TABLE public."AppUserRoles"
            ADD CONSTRAINT FK_AppUserRoles_AppUserId FOREIGN KEY ("AppUserId") REFERENCES public."AppUsers"("Id");
    END IF;
END $$;
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conrelid = 'public."AppUserRoles"'::regclass AND conname = 'fk_appuserroles_approleid'
    ) THEN
        ALTER TABLE public."AppUserRoles"
            ADD CONSTRAINT FK_AppUserRoles_AppRoleId FOREIGN KEY ("AppRoleId") REFERENCES public."AppRoles"("Id");
    END IF;
END $$;
