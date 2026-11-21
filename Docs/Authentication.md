# Authentication

[Home](README.md) > Authentication

`Collector.Auth` — JWT-based auth with email account flows.

## Pieces

- `Services/AuthService` / `SetupAuthService` / `SetupPolicies` — auth
  pipeline wiring; `Policies/AuthConstants` holds policy names.
- `Controller/AccountController` — the account endpoints.
- `RemoteAuthentication/RemoteAuth` — authenticate against a remote
  Collector instance (returns `AuthenticationResponse`).
- `Services/AuthEmailService` + `Extensions/MailExtension` — activation,
  password-reset, one-time-login email (`EmailSettings` config).

## Models

`AuthSettings` / `JwtTokenSettings` (issuer, key, lifetimes),
`LoginCredentials`, `RefreshToken`, `ActivateAccount`, `ForgotPassword`,
`ResetPassword`, `UpdatePassword`, `OneTimeLogin`, `EmailAuth`, `UserData`.

## Users

`AppUsers`, `AppRoles`, `AppUserRoles`, `AppUserTokens` tables under
`Users/` schema folder; repositories in
`Collector.Data/Repositories/Auth/`. Passwords are `Sha256`-hashed
(`Collector.Common/Encryption`). JWT access + refresh tokens; tokens are
persisted (`AppUserTokens`) for revocation.
