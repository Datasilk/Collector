CREATE TABLE IF NOT EXISTS public."AppUserRoles"
(
    "AppUserId" UUID NOT NULL,
    "AppRoleId" INT NOT NULL,
    PRIMARY KEY ("AppUserId", "AppRoleId")
);
ALTER TABLE public."AppUserRoles"
    ADD CONSTRAINT FK_AppUserRoles_AppUserId FOREIGN KEY ("AppUserId") REFERENCES public."AppUsers"("Id");
ALTER TABLE public."AppUserRoles"
    ADD CONSTRAINT FK_AppUserRoles_AppRoleId FOREIGN KEY ("AppRoleId") REFERENCES public."AppRoles"("Id");
