# Changelog

All notable changes to `Raphael.Backend`. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses
[Semantic Versioning](https://semver.org/spec/v2.0.0.html) with the NEMT criterion in
`_meta/GIT_WORKFLOW.md` §4.

**The section headings here are the release notes.** The deployment workflow reads the entry
for the tag it is publishing and refuses to create a GitHub Release without one, so an entry
missing from this file is a release that cannot go out.

Every entry carries two lines that a support ticket needs and that a list of commits does not
give:

- **Migrations** — whether the release changes the database schema. A release that does
  cannot be undone by redeploying the previous tag: the code goes back, the schema does not.
- **Clients** — the oldest version of each application this build expects. It is advice, not
  a gate: an older client is answered normally and marked with `X-Raphael-Client-Status`.

---

## [Unreleased]

---

## [1.2.0] - 2026-09-25

**Migrations:** yes — `20260923213336_AddDriverCallRequests`. It only creates two tables and
touches no existing data, so redeploying `v1.1.3` leaves them unused rather than broken.
**Clients:** unchanged — Desktop ≥1.8.1, Driver ≥1.4.0, Rider not released. The call queue needs
Desktop 1.10.0 and Driver 1.6.0; older clients never see it.

⚠️ **Deploying does not update the notification catalog.** Run
`_meta/tools/Sync-NotificationCatalog.ps1` against the environment afterwards, or the driver is
never told that the office has taken the call.

### Added

- **Drivers ask the office to call them back** (RE-026). A driver has at most one open request,
  guaranteed by a filtered unique index: pressing again adds a reminder and keeps the place in the
  queue, with a minimum interval between signals (`CallRequests:MinSecondsBetweenDriverSignals`,
  120 s). The office claims, hands over, releases, marks "no answer", resolves with a reason from a
  fixed list, and reopens. Every transition is atomic and a lost race answers 409 with who holds
  the case. The timeline is append-only and records who did what.
- Driver endpoints under `api/driver/call-requests`; office endpoints under `api/call-requests`,
  JWT only, denied to drivers, riders and integrators, and scoped to the caller's provider.
- The dispatch hub carries the queue live (`callrequests:{providerId|all}`, `CallRequestChanged`),
  with no patient data and never the resolution note.
- Business event `DRIVER_CALL_REQUEST_CLAIMED` and rule `RULE_DRIVER_CALL_REQUEST_CLAIMED_DRIVER`:
  in-app and push to the driver when the office takes the call. No names in the text.
- A request without a line is assigned the driver's line for the business day: the one with trips
  today, then the one running by its dates, then the most recent.

---

## [1.1.3] - 2026-09-19

**Migrations:** no.
**Clients:** unchanged — Desktop ≥1.8.1, Driver ≥1.4.0, Rider not released.

### Changed

- **A `Raphael.Desktop` session now lasts one working day and is never interrupted.** 1440
  minutes of access token, 1380 of refresh, a 24 hour ceiling — and the point of those
  numbers is that **nothing is ever renewed**. The refresh token is only reached for when the
  access token expires, and here the access token outlasts the session, so that moment never
  arrives. Signing in happens when the application is opened, which is the only time a
  dispatcher should be asked. Decided by the owner after being thrown out mid-shift by the
  previous settings.
  ⚠️ The refresh window is deliberately **shorter** than the access token and must stay that
  way. A renewal at 23 hours would mint a fresh 24 hour access token, and the ceiling does
  not cap the access token, so the session would run to 47 hours.
  ⚠️ What it costs, recorded rather than discovered later: an access token cannot be revoked,
  so a stolen one works for 24 hours instead of 15 minutes, and the half-hour inactivity
  window is gone — a workstation left alone stays usable for the rest of the day.

### Fixed

- **The startup check stopped crying wolf, and started saying something truer.** It warned
  whenever a refresh window was shorter than an access token, which is a fault only when the
  session is meant to outlive the token; where the two end together, as Desktop now does by
  design, nothing needs renewing. It now warns instead when an access token outlives the
  ceiling — which is a real defect, because an access token cannot be revoked and the ceiling
  is therefore not a limit but a comment. `Rider` is in exactly that state: a one-year token
  under a ninety-day ceiling.

---

## [1.1.2] - 2026-09-19

**Migrations:** no.
**Clients:** unchanged — Desktop ≥1.8.1, Driver ≥1.4.0, Rider not released.

⚠️ **Sessions on `Raphael.Desktop` behave differently, and that is the fix.** Its access token
drops from 60 minutes to 15. Nothing in any client hardcodes that number — they read
`accessTokenExpiresAtUtc` — and every copy in the field predates `X-Client-App`, so it falls
back to `Default` and is unaffected.

### Fixed

- **A Desktop session could not be renewed. Not once, for anybody.** Its policy was a 60
  minute access token against a 30 minute refresh window, and the window is only pushed
  forward when the refresh token is used — which only happens when the access token expires.
  The window had therefore always closed half an hour before anything reached for it: the
  server answered `Expired`, the client correctly ended the session, and the dispatcher was
  sent back to the sign-in screen an hour into the shift. That is precisely what refresh
  tokens were added in 1.1.0 to prevent. Desktop is now 15 and 30, which makes the intent
  written in its own configuration comment true: half an hour of inactivity ends a session,
  a working day ends it regardless.
- **`SessionPolicy:Default` had the same contradiction**, 600 against 60. It costs nothing
  today, because every client that falls back to Default predates refresh tokens and never
  asks to renew, but it was waiting for the first one that does. Now 720.
- **The comment keys in `SessionPolicy:Apps` were being read as applications.** `//Desktop`,
  `//Driver` and `//Rider` bind into the dictionary as policies carrying the class defaults.
  Invisible where a policy is looked up by name, which is every request; visible the moment
  anything iterates the dictionary.

### Added

- **The startup log names any application whose session cannot renew** — one line per
  application whose refresh window is shorter than its access token, saying so in those
  words. Nothing was checking that relationship, and it is the only one of the three that
  decides whether renewal is possible at all. It reports rather than refusing to start,
  because `Rider` violates it today with a one-year access token that cannot come down until
  the patient app that can renew is distributed.

---

## [1.1.1] - 2026-09-19

**Migrations:** no. The schema is the one 1.1.0 left.
**Clients:** unchanged — Desktop ≥1.8.1, Driver ≥1.4.0, Rider not released.

⚠️ **The application is byte for byte what 1.1.0 was.** The only difference between the two
tags is a workflow file, which is not in the published output at all. This number exists
because 1.1.0 reached production and its GitHub Release never got created, and moving a tag is
worse than spending a number.

### Fixed

- **The production pipeline asked the container that was being replaced.** 1.1.0 deployed
  correctly — production served it, and the migration it carried applied itself — and the
  pipeline reported `api.raphaeldh.com reports version '', not '1.1.0'`. An empty version, not
  a wrong one: nothing answered. Azure's deploy action returns before the application
  restarts, so `/health` answered 200 zero seconds after the deploy finished, from the process
  about to be shut down, and the single version check that followed landed in the gap.
- The version check now waits for the outgoing container to go, then asks for the expected
  version **three times running** over up to ten minutes. A deployment restarts the container
  more than once and each warm-up takes 89 to 100 seconds, so one answer proves nothing and
  one silence disproves nothing. `/health` no longer claims to prove the deployment: it stops
  at the first 200 and cannot say which container produced it.

---

## [1.1.0] - 2026-09-19

**Migrations:** yes — `AddRefreshTokens`. Additive: one new table, five indexes, two foreign
keys, one check constraint. Nothing existing is altered or dropped. It applies itself when the
API starts, which is what `Database:MigrateOnStartup` governs; redeploying 1.0.0 afterwards
would put the code back and leave the table, which is harmless because nothing in 1.0.0 reads it.
**Clients:** unchanged — Desktop ≥1.8.1, Driver ≥1.4.0, Rider not released. Every client in the
field predates `X-Client-App` and refresh tokens, and is answered exactly as before: the whole
release is expand, with no contract step.

### Added

- **A session can be renewed instead of ending.** `POST /api/Auth/refresh` exchanges a refresh
  token for a new access token and a new refresh token; `POST /api/Auth/logout` revokes one.
  Until now a token was minted at sign-in and nothing could take it back or extend it, so every
  expiry was somebody typing their password again — in the office, in the middle of a dispatch
  screen with unsaved work, and for a driver, in the middle of a route.
- **Token theft is detectable.** Refresh tokens rotate on every use and carry a family id. A
  token presented after it has already been rotated means two parties hold the same credential,
  so the whole family is revoked. A repeat within 30 seconds is treated as a retry instead, and
  that grace matters: without it, one dropped response on a phone with bad signal would sign a
  driver out mid-route.
- **Session length is configured per application**, under `SessionPolicy`. A dispatch
  workstation is a shared office machine that can be left alone with a patient's address on
  screen; a driver's phone is carried by one person through a shift. Those want opposite
  answers, and now they get them.
- **`POST /api/Auth/login` and `POST /api/Rider/auth/identify` also return** `refreshToken`,
  `accessTokenExpiresAtUtc` and `refreshTokenExpiresAtUtc`. Purely additive: a client that does
  not know these fields deserialises the response as before and ignores them.
- **The API refuses to migrate a database that has tables but no migration history.** Entity
  Framework reads an absent `__EFMigrationsHistory` as "nothing applied yet", so on a database
  that already holds data it would consider all 53 migrations pending and start creating tables
  that exist — failing partway, having already written. Measured: with this guard the process
  stops before the first statement and says what is wrong; without it, it created
  `__EFMigrationsHistory` and then died on `There is already an object named 'Trips'`. The case
  this is written for is a restore.

### Changed

- **Tokens are minted in one place.** `AuthController` and `RiderService` each built their own
  JWT, with different lifetimes and no shared notion of a session. Both now go through
  `AuthTokenService`.
- **`Jwt:ExpiresInMinutes` is superseded** by `SessionPolicy`. ⚠️ `SessionPolicy:Default`
  carries its old value of 600 on purpose: a client that sends no `X-Client-App` — which is
  every Desktop 1.8.1 and Driver 1.4.0 in the field — must keep behaving exactly as it does
  today. It drops to 60 once all three applications identify themselves and can refresh.
- **Startup migration will survive a big one.** Applying pending migrations on startup is not
  new — the API has done it for a year — but it ran with Entity Framework's 30 second command
  timeout, no retry and no lock. Migrations now run with a 300 second timeout under
  `Database:CommandTimeoutSeconds`, retry a transient SQL fault up to five times with backoff,
  and hold a session-scoped application lock so two instances cannot migrate or seed at once.
  `AddRefreshTokens` took 5.8 seconds against an empty database; an `ALTER TABLE` over a table
  holding years of trips is the case these numbers exist for, and 30 seconds would have failed
  it halfway and stopped the API from starting.
- **`Database:MigrateOnStartup` is written down** in `appsettings.json` and as an App Service
  setting in both environments, instead of being inherited from the default in the code. The
  value is the same, `true`. What changes is that in production a schema change on an
  unrequested recycle is now something somebody decided.

### Fixed

- **A token was accepted for five minutes after it expired.** `ClockSkew` was left at its
  default, which is applied silently, so every configured lifetime was five minutes longer than
  it said. Measured: a token issued with a one-minute lifetime was still accepted seventy
  seconds later. Now 30 seconds, enough for a request already in flight and no more. Harmless
  while the number was ten hours; not harmless once it is sixty minutes.

### Removed

- **`POST /api/Auth/loginTest`**, which was anonymous and returned `{"message":"Login
  successful"}` without looking at anything.

---

## [1.0.0] - 2026-09-18

The first named build of `Raphael.Backend`, and the one that starts production on Azure.

Until now this repository had no version. It committed straight to `main`, was uploaded by
hand to MyASP.NET, and the answer to "which version is in production?" was somebody's
recollection of the last upload. Every release from here is a tag, deployed by CI, and the
running API says which one it is.

### Added

- **`GET /api/version`** — the build serving the request and which environment it is.
  Anonymous, so an application can check it is current before anybody signs in and a support
  ticket can name the build without credentials.
- **`GET /api/version/details`** — behind authentication: the commit, when CI produced the
  build, the last migration the database reports, the contract versions, and the client
  versions this build expects.
- **Client identification.** The applications may send `X-Client-App` and `X-Client-Version`.
  Both are optional, and what arrives is stamped on the request in Application Insights, so a
  fault can be sliced by the version of the application that hit it. Nothing is refused for
  failing to send them.
- **An outdated client is told so.** Below the floor configured in `ClientCompatibility`, the
  response carries `X-Raphael-Client-Status: outdated` and `X-Raphael-Client-Minimum`. It is
  never a refusal: leaving a driver mid-route with an application that will not load is worse
  than anything an old version gets wrong.
- **The build stamps itself.** `Directory.Build.props` declares the version; CI injects the
  commit and the build time. Application Insights reads the version into
  `application_Version` on its own, so every trace and exception in production now carries the
  build that raised it.

### Changed

- **Swagger no longer answers in production.** It stays in every other environment. It
  publishes the whole shape of the system — every controller, every schema, and the existence
  and header name of both machine-to-machine keys — and it was being served anonymously
  everywhere, in the week production starts holding real patient data. Integrators explore
  against DEV; their contract is `_meta/INTEGRATION_API_SPEC.md`, which is what they code
  against in any case.
- **The version is deployed by tag, not by push.** `main` continues to deploy to DEV on every
  push. Production is deployed only by pushing `vX.Y.Z`, through an environment that requires
  a reviewer, and the workflow refuses a tag that disagrees with the declared version or that
  is not on `main`.

### Fixed

- Swagger was registered three times: once unconditionally, once inside a check for
  `Development` that therefore did nothing, and once more under a comment claiming it was for
  production.

### Security

- The API surface is no longer published anonymously from the host that serves patient data.
- `GET /api/version` is anonymous by explicit decision and carries two fields. Everything that
  describes the inside of the system — commit, build time, schema — needs a token, the same
  reasoning that puts `/health/ready` behind authentication.

### Deployment

- **Migrations: yes.** `20260918070955_DropOrphanPhoneColumn`, which is not in the production
  backup of 2026-09-17 that this environment was built from. It is written as guarded raw SQL
  rather than `DropColumn`, so it does nothing where the column is already absent — which is
  the case in every database in real use. Redeploying an earlier build after this one would
  not restore the column, and nothing maps it.
- **Clients:** `Raphael.Desktop` 1.8.1 · `Raphael.Driver` 1.4.0 · `raphael-rider` not yet
  released, so no floor is set.
- **Note:** none of the three applications sends `X-Client-App` yet, so the compatibility
  answer is silent until they do. The server half is here so that the first application to
  ship it is useful on its own.
