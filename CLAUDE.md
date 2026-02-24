# OpenShock API Backend

## Project Overview
OpenShock backend: REST API, real-time WebSocket gateway (LCG), and scheduled jobs (Cron). Controls shock devices via ESP32 hubs over FlatBuffers WebSocket protocol with Redis pub/sub message routing.

## Tech Stack
- .NET 10, ASP.NET Core (SlimBuilder), C# latest
- PostgreSQL via Npgsql + EF Core (DbContext pooling + factory)
- Redis Stack (Redis.OM for documents, RediSearch indexing, pub/sub, keyspace notifications)
- SignalR (WebSockets only, custom Redis backplane with `local#` prefix optimization)
- FlatBuffers (hub↔LCG binary WebSocket protocol)
- MessagePack (Redis pub/sub serialization)
- Serilog (console + Grafana Loki + OpenTelemetry)
- OpenTelemetry (Prometheus exporter at `/metrics`)
- Asp.Versioning — URL-based: `/{version:apiVersion}/...`
- OneOf discriminated unions for service return types
- BCrypt.Net for password hashing
- Fluid (Liquid) for email templates
- HybridCache (memory + distributed, 5-min expiry)
- TUnit for tests, Testcontainers for integration tests

## Solution Projects

| Project | Purpose |
|---|---|
| `Common` | Shared: DB context, entities, auth handlers, hubs, Redis models, services, middleware |
| `API` | Main REST API for user-facing operations |
| `LiveControlGateway` | WebSocket gateway for real-time hub (ESP32) connections |
| `Cron` | Hangfire scheduled jobs |
| `MigrationHelper` | Standalone EF Core migration tooling |
| `SeedE2E` | E2E test data seeder (Bogus fakers) |
| `API.IntegrationTests` | TUnit integration tests |
| `Common.Tests` | TUnit unit tests for Common |

## Project Structure

### Common (`Common/`)
```
Authentication/
  AuthenticationHandlers/     # UserSession, ApiToken, Hub auth handlers
  ControllerBase/             # AuthenticatedSessionControllerBase (IActionFilter)
  Services/                   # IUserReferenceService
  Attributes/                 # TokenPermissionAttribute
Constants/                    # AuthConstants, HardLimits
DeviceControl/                # ControlSender, device command routing
Errors/                       # Static error factories
ExceptionHandle/              # OpenShockExceptionHandler (IExceptionHandler)
Extensions/                   # ConfigurationExtensions, HttpContextExtensions
Hubs/                         # UserHub, PublicShareHub + interfaces
JsonSerialization/            # JsonOptions, SemVersionJsonConverter
Migrations/                   # EF Core migrations
Models/                       # Domain enums (PermissionType, RoleType, ControlType, ShockerModelType)
Models/WebSocket/             # WebSocket message DTOs
OpenShockDb/                  # OpenShockContext + all entity classes
Options/                      # DatabaseOptions, FrontendOptions, MetricsOptions, etc.
Problems/                     # OpenShockProblem (RFC 7807), ValidationProblem
Redis/                        # DeviceOnline, LoginSession, DevicePair, LcgNode (Redis.OM docs)
Redis/PubSub/                 # MessagePack models for device messages
Services/                     # SessionService, ControlSender, BatchUpdateService, etc.
Utils/                        # HashingUtils, CryptoUtils, GravatarUtils
Validation/                   # CharsetMatchers, UsernameValidator
Websocket/                    # WebsocketBaseController<T>, FlatbuffersWebsocketBaseController
OpenShockServiceHelper.cs     # Central service registration (DB, Redis, auth, rate limiting)
OpenShockControllerBase.cs    # Base controller with Problem(), LegacyDataOk()
```

### API (`API/`)
```
Controller/
  Account/                    # Login(V1/V2), Signup(V1/V2), Logout, Activate, PasswordReset
  Account/Authenticated/      # ChangeEmail, ChangePassword, ChangeUsername, Deactivate
  Admin/                      # User management, config, webhooks, blacklists
  Device/                     # Hub-authenticated: GetSelf, Pair, AssignLCG
  Devices/                    # User-authenticated: CRUD, OTA, shockers, pair codes
  OAuth/                      # Discord/Google/Twitter OAuth flow
  Public/                     # Stats, public share links
  Sessions/                   # List, delete, self
  Shares/                     # Public links, user shares, share codes
  Shockers/                   # CRUD, control, logs, pause, share management
  Tokens/                     # API token CRUD, self, reporting
  Users/                      # GetSelf, LookupByName
  Version/                    # GET / — server info
Errors/                       # API-specific error statics
Services/                     # AccountService, TurnstileService, EmailService, etc.
Realtime/RedisSubscriberService.cs  # Hosted service: Redis keyspace + device-status listener
```

### LiveControlGateway (`LiveControlGateway/`)
```
Controllers/
  HubV1Controller.cs          # /{v}/ws/device — FlatBuffers v1 (HubToken auth)
  HubV2Controller.cs          # /{v}/ws/device — FlatBuffers v2 (HubToken auth)
  LiveControlController.cs    # /{v}/ws/live/{hubId} — JSON WebSocket (user auth)
LifetimeManager/              # HubLifetimeManager — tracks connected hubs, routes commands
```

## Key Architectural Patterns

### Partial Controllers
Each controller is a `sealed partial class`. `_ApiController.cs` declares the class with attributes + DI fields. Individual actions are separate `.cs` files as partial continuations.

### OneOf Discriminated Unions
Service methods return `OneOf<Success, Error1, Error2, ...>`. Callers use `.Match()` or `.TryPickT0()`. No exceptions for expected failure paths.

### RFC 7807 Problem Details
All errors use `OpenShockProblem` (extends `ProblemDetails`). Static factory classes create specific instances. `.ToObjectResult()` for controllers, `.WriteAsJsonAsync()` for auth handlers/WebSocket contexts.

### WebSocket Base Controllers
`WebsocketBaseController<T>` uses `Channel<T>` for thread-safe send queuing with background `MessageLoop`. `FlatbuffersWebsocketBaseController` extends it for binary FlatBuffers (LCG hub connections).

### BatchUpdateService
Singleton that batches async `last_used` timestamp updates for sessions and API tokens to prevent N+1 DB writes.

## Authentication

| Scheme | Header/Cookie | Handler | Notes |
|---|---|---|---|
| `UserSessionCookie` | Cookie `openShockSession` or header `OpenShockSession` | Looks up `LoginSession` in Redis → user from DB | Default scheme, expands TTL |
| `ApiToken` | Header `OpenShockToken` | SHA-256 hash lookup in DB | Permission-checked via `TokenPermissionAttribute` |
| `HubToken` | Header `DeviceToken` | Looks up `Device` in DB | For ESP32 hubs |

Combined scheme `"UserSessionCookie,ApiToken"` for endpoints accepting either.

All handlers write RFC 7807 JSON on 401 challenge (no redirect).

## Database (PostgreSQL + EF Core)

Key entities: `User`, `Device`, `Shocker`, `ApiToken`, `UserShare`, `PublicShare`, `ShockerControlLog`, `DeviceOtaUpdate`, `LoginSession` (Redis), `DeviceOnline` (Redis)

PostgreSQL enums: `control_type`, `permission_type`, `role_type`, `shocker_model_type`, `ota_update_status`

Collation: `ndcoll` (ICU case-insensitive) on `users.name` and username blacklist.

Both `AddDbContextPool` and `AddPooledDbContextFactory` registered (factory used by LCG per-message scopes).

## Redis

Four Redis.OM indexed document types:
- `LoginSession` — user sessions with TTL
- `DeviceOnline` — connected device state (TTL-based offline detection via keyspace notifications)
- `DevicePair` — pairing code ↔ device mapping
- `LcgNode` — active LCG gateway nodes

Pub/sub channels:
- `device-msg:{deviceId}` — control commands → consumed by LCG
- `device-status` — online/offline events → consumed by API's `RedisSubscriberService`

## SignalR Hubs

| Hub | Path | Auth |
|---|---|---|
| `UserHub` | `/1/hubs/user` | UserSessionCookie or ApiToken |
| `PublicShareHub` | `/1/hubs/share/link/{id:guid}` | Optional session |

WebSockets only. Custom `OpenShockRedisHubLifetimeManager` supports `local#` prefix for same-node optimization.

## Configuration

Environment variable prefix: `OPENSHOCK__` (double underscore for nested). Key sections:
- `OpenShock:DB` → `DatabaseOptions` (Conn, SkipMigration)
- `OpenShock:Redis` → connection (Conn string or Host/Port/User/Password)
- `OpenShock:Frontend` → `FrontendOptions` (BaseUrl, ShortUrl, CookieDomain — comma-separated)
- `OpenShock:Turnstile` → `TurnstileOptions` (Enabled, SecretKey, SiteKey)
- `OpenShock:Mail` → `MailOptions` (type: MAILJET or SMTP)

Under integration tests (`ASPNETCORE_UNDER_INTEGRATION_TEST=1`): only environment variables loaded.

## Rate Limiting

Global sliding window: 1000 req/min unauthenticated, 120 req/min per user, unlimited for Admin/System.
Named policies: `"auth"` (10/min fixed window by IP), `"token-reporting"` (concurrency 5), `"shocker-logs"` (concurrency 10).

## Commands

```bash
# Build
dotnet build API/API.csproj
dotnet build LiveControlGateway/LiveControlGateway.csproj
dotnet build Cron/Cron.csproj

# Test (requires Docker for Testcontainers)
dotnet test Common.Tests/Common.Tests.csproj
dotnet test API.IntegrationTests/API.IntegrationTests.csproj

# Migrations
dotnet ef migrations add <Name> --project Common --startup-project MigrationHelper

# Docker
docker build -f docker/API.Dockerfile .
docker build -f docker/LiveControlGateway.Dockerfile .
docker build -f docker/Cron.Dockerfile .
```

## CI/CD
- `ci-build.yml`: test → build Docker → promote → deploy (master→prod, develop→staging)
- `ci-tag.yml`: semver tag releases, multi-arch (amd64+arm64)
- Images: `ghcr.io/openshock/{api,live-control-gateway,cron}`
- GitOps: dispatch to `openshock/kubernetes-cluster-gitops`

## Integration Test Patterns
- TUnit with `[ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]`
- Testcontainers: PostgreSQL + Redis Stack (shared per session)
- `InterceptedHttpMessageHandler` mocks Cloudflare Turnstile (`"valid-token"` → success) and MailJet
- `TestHelper` bypasses signup/login for test setup (direct DB + Redis session creation)
- Rate limiting disabled in test host
- Cookie domain includes `localhost` for test server
- Auth helpers: `CreateAuthenticatedClient`, `CreateApiTokenClient`, `CreateHubTokenClient`
