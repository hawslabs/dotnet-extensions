# Fabric Wolverine POC Implementation Plan

Updated: 2026-05-31

## 0. Purpose

Create a prototype named **Fabric** that demonstrates a Windows-friendly, SQL Server-backed, Wolverine-powered, SignalR-connected messaging and federation model for the current application.

The prototype must prove these scenarios:

1. A cloud hub cluster accepts cloud workers and connected edge installations.
2. An edge installation hub accepts local workers and user/client connections.
3. An edge installation hub can optionally connect outward to the cloud hub by using Wolverine's `UseClientToSignalR` transport when cloud features are enabled.
4. An edge installation can optionally connect outward to a parent installation hub by using the same SignalR client transport model.
5. Local development can run the whole topology from Aspire AppHost.
6. Local development can identify hubs, workers, and test clients with development query values or headers instead of JWT setup.
7. Non-local development uses JWT bearer identity.
8. Edge installations operate locally without cloud connectivity.
9. Workers communicate through SignalR for near-real-time commands, queries, results, and events.
10. Durable processing is stored in SQL Server through Wolverine durability.
11. Cloud services use .NET `HybridCache`, backed by Redis.
12. Installation boundaries are enforced from connection identity and route authorization.
13. Public routing uses `InstallationId + WorkerRole`, plus `UserId` only for user-targeted messages.
14. No routing, persistence, or public identity model depends on a generated process identifier.
15. The current application inventory is implemented as generated contracts and deterministic stub handlers:
    - 190 commands with matching results.
    - 170 queries with matching results.
    - 8 bot RPC commands.
    - 33 events.
    - 11 asynchronous/background work messages.

The prototype should prefer clarity over abstraction. It should be easy for Codex/Copilot to generate, run, debug, and extend.

---

## 1. Required solution layout

Generate this layout:

```text
poc-wolverine
├── cloud
│   ├── contracts
│   │   └── Fabric.Cloud.Contracts.csproj
│   ├── hub
│   │   └── Fabric.Cloud.Hub.csproj
│   ├── worker-a
│   │   └── Fabric.Cloud.WorkerA.csproj
│   └── worker-b
│       └── Fabric.Cloud.WorkerB.csproj
├── edge
│   ├── contracts
│   │   └── Fabric.Edge.Contracts.csproj
│   ├── hub
│   │   └── Fabric.Edge.Hub.csproj
│   ├── worker-a
│   │   └── Fabric.Edge.WorkerA.csproj
│   └── worker-b
│       └── Fabric.Edge.WorkerB.csproj
├── hosting
│   ├── app-host
│   │   └── Fabric.AppHost.csproj
│   └── service-defaults
│       └── Fabric.ServiceDefaults.csproj
├── Directory.Build.props
├── Directory.Packages.props
└── Fabric.slnx
```

### Naming rules

Use these names everywhere:

```text
Fabric
Fabric.Cloud.Contracts
Fabric.Cloud.Hub
Fabric.Cloud.WorkerA
Fabric.Cloud.WorkerB
Fabric.Edge.Contracts
Fabric.Edge.Hub
Fabric.Edge.WorkerA
Fabric.Edge.WorkerB
Fabric.AppHost
Fabric.ServiceDefaults
AddFabricHub
AddFabricWorker
UseFabricHub
UseFabricWorker
UseFabricOutboundLinks
FabricGroups
FabricEnvelope
FabricRouteTarget
WorkerRole
```

Do not use legacy `*.Service` project names.

Do not use legacy tenancy-named APIs or groups. The product terminology is **installation**.

Do not create legacy source/example wrapper folders for this POC. The contracts/runtime code should be placed inside the projects in the requested cloud/edge/hosting layout.

The initial POC does not create a browser UI project. User/client SignalR flows are covered by test clients and diagnostics endpoints. A UI project can be added later without changing the core Fabric protocol.

---

## 2. Project responsibilities

| Project                  | Responsibility                                                                                                                                                                                              |
| ------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Fabric.Edge.Contracts`  | Shared Fabric primitives that must be safe to deploy to customer machines, plus edge-facing message contracts.                                                                                              |
| `Fabric.Cloud.Contracts` | Cloud-only contracts and generated catalog contracts that do not need to be deployed to every edge install. It may reference `Fabric.Edge.Contracts` for shared primitives.                                 |
| `Fabric.Edge.Hub`        | Local coordination authority for one installation. Hosts the local SignalR hub, accepts edge workers, accepts user/test clients, uses SQL durability, and optionally connects outward to cloud/parent hubs. |
| `Fabric.Edge.WorkerA`    | Edge worker role `MilestoneProxy`. Owns VMS/Milestone command/query stubs.                                                                                                                                  |
| `Fabric.Edge.WorkerB`    | Edge worker roles `MilestoneLiveEvents` and `ServiceHost` for the POC. Owns live monitoring and local service-host/background stubs.                                                                        |
| `Fabric.Cloud.Hub`       | Cloud-side SignalR hub and coordination process. Accepts cloud workers and edge hubs connected outward as `WorkerRole.ServiceHost`.                                                                         |
| `Fabric.Cloud.WorkerA`   | Cloud worker roles `CloudCore` and `CloudAIAnalysis`. Owns core/provisioning and AI-analysis stubs.                                                                                                         |
| `Fabric.Cloud.WorkerB`   | Cloud worker roles `CloudLicensing` and `CloudBotIntegration`. Owns licensing, license portal, and bot integration stubs.                                                                                   |
| `Fabric.AppHost`         | Aspire local topology. Starts SQL Server 2025, Redis, cloud hubs/workers, and multiple edge installations.                                                                                                  |
| `Fabric.ServiceDefaults` | Shared OpenTelemetry, health checks, resilience, service discovery, and common .NET host defaults.                                                                                                          |

Contract split rule:

```text
Contracts are allowed on customer machines.
Cloud runtime behavior is not allowed in edge/customer-deployed projects.
Edge runtime behavior is not allowed in cloud-only projects unless it is a shared contract/DTO.
No executable project should define message contracts privately if another process must send or receive them.
```

---

## 3. Core terminology

### Installation

An **installation** is a logical deployment boundary.

Examples:

```text
company-cloud
edge-01
edge-02
```

`edge-01` and `edge-02` are local POC instance names only. They do not imply different software versions, roles, capabilities, release channels, or deployment tracks. Each edge instance runs the same `Fabric.Edge.*` code with a different installation id, database, resource name, and connection configuration.

Use a stable `Guid` as the installation id.

Prototype ids:

```csharp
public static class FabricInstallations
{
    public static readonly Guid Cloud = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid Edge01 = Guid.Parse("00000000-0000-0000-0000-000000000101");
    public static readonly Guid Edge02 = Guid.Parse("00000000-0000-0000-0000-000000000102");
}
```

### Hub

A **hub** is an ASP.NET Core process that exposes the Fabric SignalR hub.

Examples:

```text
Fabric.Cloud.Hub
Fabric.Edge.Hub
```

An edge hub may also act as a SignalR client to another hub.

### Worker

A **worker** is a process that connects to a hub through Wolverine's SignalR client transport.

Examples:

```text
Fabric.Cloud.WorkerA
Fabric.Cloud.WorkerB
Fabric.Edge.WorkerA
Fabric.Edge.WorkerB
```

### Connection

A **connection** is one active SignalR connection.

Connections are tracked for diagnostics, leases, presence, request/reply correlation, and delivery ownership.

Connections are not the public routing model.

Public routing uses:

```text
installation id
worker role
user id when targeting a user
logical route target
```

---

## 4. Runtime shape

```text
Cloud installation: company-cloud
  ├─ Fabric.Cloud.Hub x2
  │   ├─ ASP.NET Core process
  │   ├─ Fabric SignalR hub
  │   ├─ Wolverine SQL Server durability
  │   ├─ EF Core cloud state
  │   ├─ Redis-backed HybridCache
  │   ├─ cloud message handlers
  │   ├─ accepts cloud worker SignalR clients
  │   └─ accepts edge hub SignalR clients when cloud features are enabled
  │
  ├─ Fabric.Cloud.WorkerA
  │   ├─ WorkerRole.CloudCore
  │   ├─ WorkerRole.CloudAIAnalysis
  │   ├─ SignalR client connection to cloud hub
  │   ├─ Wolverine runtime
  │   └─ core/provisioning/AI stubs
  │
  └─ Fabric.Cloud.WorkerB
      ├─ WorkerRole.CloudLicensing
      ├─ WorkerRole.CloudBotIntegration
      ├─ SignalR client connection to cloud hub
      ├─ Wolverine runtime
      └─ licensing/bot stubs

Edge installation: any configured edge instance, such as edge-01 or edge-02
  ├─ Fabric.Edge.Hub
  │   ├─ ASP.NET Core process
  │   ├─ local Fabric SignalR hub
  │   ├─ Wolverine SQL Server durability
  │   ├─ EF Core edge state
  │   ├─ local edge message handlers
  │   ├─ accepts local edge worker SignalR clients
  │   ├─ accepts user/test client SignalR connections
  │   ├─ optional outbound parent installation SignalR client link
  │   └─ optional outbound cloud SignalR client link using UseClientToSignalR
  │
  ├─ Fabric.Edge.WorkerA
  │   ├─ WorkerRole.MilestoneProxy
  │   ├─ SignalR client connection to edge hub
  │   └─ milestone-proxy stubs
  │
  └─ Fabric.Edge.WorkerB
      ├─ WorkerRole.MilestoneLiveEvents
      ├─ WorkerRole.ServiceHost for local service-host/background stubs in the POC
      ├─ SignalR client connection to edge hub
      └─ live-monitoring and service-host stubs
```

Key split:

```text
SignalR = connected near-real-time transport
SQL Server + Wolverine durability = reliable processing, retry, inbox/outbox, scheduled work
EF Core = prototype business state and audit tables
Redis + HybridCache = cloud cache only
Local-dev query values/headers = simple identity in local development
JWT = authenticated source identity outside local development
Fabric envelope = route, installation, correlation, version, permissions, and contract metadata
Aspire AppHost = local development orchestrator, not production packaging
```

Do not rely on SignalR alone for durable business guarantees.

SignalR gives online delivery. Wolverine + SQL Server gives durable processing. Fabric adds message ids, acknowledgements, deduplication, retry rules, connection leases, and installation-aware routing around cross-process SignalR communication.

---

## 5. Local Aspire topology

`Fabric.AppHost` is the default full-stack local development entry point.

It must start:

```text
Fabric.AppHost
  ├─ sql -> SQL Server 2025 Docker container
  │   ├─ FabricCloud
  │   ├─ FabricEdge01
  │   └─ FabricEdge02
  │
  ├─ redis -> Redis container for cloud HybridCache
  │
  ├─ company-cloud installation
  │   ├─ cloud-hub-01    -> Fabric.Cloud.Hub, WorkerRole.CloudHub
  │   ├─ cloud-hub-02    -> Fabric.Cloud.Hub, WorkerRole.CloudHub
  │   ├─ cloud-worker-a  -> Fabric.Cloud.WorkerA, WorkerRole.CloudCore + WorkerRole.CloudAIAnalysis
  │   └─ cloud-worker-b  -> Fabric.Cloud.WorkerB, WorkerRole.CloudLicensing + WorkerRole.CloudBotIntegration
  │
  ├─ edge-01 installation instance
  │   ├─ edge-01-hub      -> Fabric.Edge.Hub, WorkerRole.EdgeHub
  │   ├─ edge-01-worker-a -> Fabric.Edge.WorkerA, display=edge-worker-a, WorkerRole.MilestoneProxy
  │   └─ edge-01-worker-b -> Fabric.Edge.WorkerB, display=edge-worker-b, WorkerRole.MilestoneLiveEvents
  │
  └─ edge-02 installation instance
      ├─ edge-02-hub      -> Fabric.Edge.Hub, WorkerRole.EdgeHub
      ├─ edge-02-worker-a -> Fabric.Edge.WorkerA, display=edge-worker-a, WorkerRole.MilestoneProxy
      └─ edge-02-worker-b -> Fabric.Edge.WorkerB, display=edge-worker-b, WorkerRole.MilestoneLiveEvents
```

Both edge instances run the same edge hub and worker projects. The instance suffix is only needed so Aspire resources, databases, URLs, and installation ids are unique in the local POC topology.

### Cluster semantics

SignalR connections are process-local.

For local clustering, use SQL Server-backed Fabric records and Wolverine durability to coordinate work that cannot be delivered directly from the current hub process.

Cluster dispatch rule:

```text
If target connection is attached to the current hub process:
  deliver over local SignalR immediately.

If target connection is attached to another hub process in the same logical cluster:
  persist/queue work through SQL Server.
  the owning hub process claims the work and sends over its local SignalR connection.

If no matching connection is attached:
  keep durable delivery pending until a matching connection connects or the message expires.
```

### Edge-to-cloud connection rule

When cloud features are enabled, `Fabric.Edge.Hub` connects outward to the cloud hub.

This avoids inbound firewall requirements for customer installations.

The edge hub identifies itself to the cloud as:

```text
InstallationId = edge installation id
Role = WorkerRole.ServiceHost
DisplayName = edge installation name
```

The cloud can route to a given edge installation's service host with:

```csharp
FabricGroups.InstallationWorker(edgeInstallationId, WorkerRole.ServiceHost)
```

---

## 6. AppHost extension methods

Create extension methods in:

```text
hosting/app-host
├── Extensions
│   ├── FabricDistributedApplicationExtensions.cs
│   ├── FabricAppHostResources.cs
│   └── FabricAppHostOptions.cs
└── Fabric.AppHost.csproj
```

Use the exact Aspire resource type names generated by the installed Aspire SDK. The following signatures show intent.

### Resource records

```csharp
public sealed record CloudCommonServicesResource(
    IResourceBuilder<SqlServerServerResource> Sql,
    IResourceBuilder<SqlServerDatabaseResource> CloudDatabase,
    IResourceBuilder<RedisResource> Redis);

public sealed record CloudInstanceResources(
    string Name,
    Guid InstallationId,
    IReadOnlyList<IResourceBuilder<ProjectResource>> Hubs,
    IResourceBuilder<ProjectResource> WorkerA,
    IResourceBuilder<ProjectResource> WorkerB,
    IResourceBuilder<ProjectResource> PrimaryHub);

public sealed record EdgeInstanceResources(
    string Name,
    Guid InstallationId,
    IResourceBuilder<SqlServerDatabaseResource> Database,
    IResourceBuilder<ProjectResource> Hub,
    IResourceBuilder<ProjectResource> WorkerA,
    IResourceBuilder<ProjectResource> WorkerB);
```

### Option records

```csharp
public sealed record CloudInstanceOptions
{
    public string Name { get; init; } = "company-cloud";
    public Guid InstallationId { get; init; } = FabricInstallations.Cloud;
    public int HubCount { get; init; } = 2;
}

public sealed record EdgeInstanceOptions
{
    public required string Name { get; init; }
    public required Guid InstallationId { get; init; }
    public required string DatabaseName { get; init; }
    public bool CloudEnabled { get; init; } = true;
}
```

### Extension signatures

```csharp
public static class FabricDistributedApplicationExtensions
{
    public static CloudCommonServicesResource AddCloudCommonServices(
        this IDistributedApplicationBuilder builder,
        string sqlName = "sql",
        string redisName = "redis");

    public static CloudInstanceResources AddCloudInstance(
        this IDistributedApplicationBuilder builder,
        CloudCommonServicesResource common,
        Action<CloudInstanceOptions>? configure = null);

    public static EdgeInstanceResources AddEdgeInstance(
        this IDistributedApplicationBuilder builder,
        CloudCommonServicesResource common,
        CloudInstanceResources cloud,
        Action<EdgeInstanceOptions> configure);
}
```

### `AddCloudCommonServices(...)`

Behavior:

```text
Create one SQL Server 2025 container.
Create the cloud database.
Create one Redis container.
Return references to SQL, cloud database, and Redis.
```

Implementation outline:

```csharp
public static CloudCommonServicesResource AddCloudCommonServices(
    this IDistributedApplicationBuilder builder,
    string sqlName = "sql",
    string redisName = "redis")
{
    var sql = builder.AddSqlServer(sqlName)
        .WithImageTag("2025-latest")
        .WithDataVolume("fabric-sql-data");

    var cloudDatabase = sql.AddDatabase("FabricCloud");

    var redis = builder.AddRedis(redisName)
        .WithDataVolume("fabric-redis-data");

    return new CloudCommonServicesResource(sql, cloudDatabase, redis);
}
```

### `AddCloudInstance(...)`

Behavior:

```text
Create one logical cloud installation.
Create two cloud hub resources by default.
Create cloud worker A and cloud worker B.
Attach all cloud services to SQL Server and Redis.
Configure cloud services to use HybridCache.
Return references to hubs and workers.
```

Environment conventions:

```text
Fabric__Auth__Mode=DevelopmentIdentity
Fabric__Identity__InstallationId=<cloud installation id>
Fabric__Identity__Role=<WorkerRole>
Fabric__Identity__DisplayName=<display name>
Fabric__SignalR__HubPath=/fabric/messages
Fabric__Cloud__AllowedInstallations__0=<edge-01>
Fabric__Cloud__AllowedInstallations__1=<edge-02>
```

Cloud worker hub URL convention:

```text
Fabric__Hub__Url=http://127.0.0.1:<selected cloud hub port>/fabric/messages
```

Default resources:

```text
cloud-hub-01
cloud-hub-02
cloud-worker-a
cloud-worker-b
```

### `AddEdgeInstance(...)`

Behavior:

```text
Create one edge database under the shared SQL Server.
Create one edge hub resource.
Create edge worker A and edge worker B.
Configure edge hub to connect outward to a selected cloud hub when cloud is enabled.
Return references to edge hub and workers.
```

Global Aspire resource names must be installation-scoped:

```text
{edge-name}-hub
{edge-name}-worker-a
{edge-name}-worker-b
```

Fabric display names remain stable:

```text
edge-worker-a
edge-worker-b
```

### AppHost `Program.cs`

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var common = builder.AddCloudCommonServices();

var cloud = builder.AddCloudInstance(
    common,
    options =>
    {
        options.Name = "company-cloud";
        options.InstallationId = FabricInstallations.Cloud;
    });

var edge01 = builder.AddEdgeInstance(
    common,
    cloud,
    options =>
    {
        options.Name = "edge-01";
        options.InstallationId = FabricInstallations.Edge01;
        options.DatabaseName = "FabricEdge01";
    });

var edge02 = builder.AddEdgeInstance(
    common,
    cloud,
    options =>
    {
        options.Name = "edge-02";
        options.InstallationId = FabricInstallations.Edge02;
        options.DatabaseName = "FabricEdge02";
    });

builder.Build().Run();
```

---

## 7. Target frameworks and package strategy

Use `net10.0` for all new POC projects except where a later real customer bridge might require .NET Framework.

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

Use central package management.

`Directory.Packages.props` baseline:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="WolverineFx" Version="5.*" />
    <PackageVersion Include="WolverineFx.SqlServer" Version="5.*" />
    <PackageVersion Include="WolverineFx.EntityFrameworkCore" Version="5.*" />
    <PackageVersion Include="WolverineFx.SignalR" Version="5.*" />

    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.*" />
    <PackageVersion Include="Microsoft.AspNetCore.SignalR.Client" Version="10.*" />
    <PackageVersion Include="Microsoft.Extensions.Caching.Hybrid" Version="10.*" />
    <PackageVersion Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.*" />

    <PackageVersion Include="Aspire.Hosting.AppHost" Version="13.*" />
    <PackageVersion Include="Aspire.Hosting.SqlServer" Version="13.*" />
    <PackageVersion Include="Aspire.Hosting.Redis" Version="13.*" />

    <PackageVersion Include="xunit.v3" Version="3.*" />
    <PackageVersion Include="FluentAssertions" Version="8.*" />
  </ItemGroup>
</Project>
```

If exact package names differ, use the current Wolverine/Aspire/.NET package names that expose the required APIs.

---

## 8. Shared Fabric primitives

Place shared primitives in `Fabric.Edge.Contracts/Common` so customer-deployed code can compile contracts and clients without cloud runtime dependencies.

`Fabric.Cloud.Contracts` may reference `Fabric.Edge.Contracts`.

```text
edge/contracts
└── Common
    ├── IFabricCommand.cs
    ├── IFabricQuery.cs
    ├── IFabricEvent.cs
    ├── IFabricResult.cs
    ├── IFabricSocketMessage.cs
    ├── IFabricMessage.cs
    ├── FabricResult.cs
    ├── ProblemDetailsDto.cs
    ├── FabricEnvelope.cs
    ├── FabricRouteTarget.cs
    ├── FabricGroups.cs
    ├── FabricConnectionPrincipal.cs
    ├── WorkerRole.cs
    ├── ContractVersions.cs
    └── FabricInstallations.cs
```

Runtime helpers live in the executable projects or in `Fabric.ServiceDefaults` when they are safe for all hosts:

```text
hosting/service-defaults
├── Configuration
│   ├── FabricHubOptions.cs
│   ├── FabricWorkerOptions.cs
│   ├── FabricIdentityOptions.cs
│   ├── FabricAuthOptions.cs
│   ├── FabricCloudLinkOptions.cs
│   ├── FabricParentLinkOptions.cs
│   ├── FabricSqlServerOptions.cs
│   ├── FabricSignalROptions.cs
│   └── FabricHybridCacheOptions.cs
├── Identity
│   ├── IFabricIdentityResolver.cs
│   ├── DevelopmentFabricIdentityResolver.cs
│   ├── JwtFabricIdentityResolver.cs
│   └── FabricDevelopmentIdentityQuery.cs
├── SignalR
│   ├── FabricHub.cs
│   ├── FabricWireMessage.cs
│   ├── FabricWireMessageSerializer.cs
│   ├── FabricSignalRClientUrlBuilder.cs
│   └── FabricSignalRPublishExtensions.cs
├── Durability
│   ├── FabricDbContext.cs
│   ├── FabricConnectionLease.cs
│   ├── FabricPendingDelivery.cs
│   ├── FabricProcessedMessage.cs
│   ├── FabricDeliveryAttempt.cs
│   └── FabricSchema.cs
├── Wolverine
│   ├── FabricHubRegistrationExtensions.cs
│   ├── FabricWorkerRegistrationExtensions.cs
│   ├── WolverineFabricExtensions.cs
│   └── FabricWolverinePolicies.cs
└── Diagnostics
    ├── FabricHealthChecks.cs
    └── FabricMetrics.cs
```

---

## 9. Public registration API

### Hub registration

```csharp
public static class FabricHubRegistrationExtensions
{
    public static IServiceCollection AddFabricHub(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<FabricHubOptions>? configure = null);
}
```

`AddFabricHub` should:

1. Bind `Fabric` configuration.
2. Register `FabricHubOptions`.
3. Register identity resolution.
4. Register route authorization.
5. Register EF Core `FabricDbContext` for support tables.
6. Register the custom `FabricHub`.
7. Register SignalR.
8. Register diagnostics.

`AddFabricHub` should not call `UseWolverine` directly because Wolverine is configured on `IHostBuilder` / `HostApplicationBuilder`.

### Worker registration

```csharp
public static class FabricWorkerRegistrationExtensions
{
    public static IServiceCollection AddFabricWorker(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<FabricWorkerOptions>? configure = null);
}
```

`AddFabricWorker` should:

1. Bind identity and hub options.
2. Register identity providers.
3. Register a SignalR URL builder.
4. Register worker diagnostics.

### Wolverine extensions

```csharp
public static class WolverineFabricExtensions
{
    public static WolverineOptions UseFabricHub(
        this WolverineOptions options,
        IConfiguration configuration,
        IServiceProvider? serviceProvider = null);

    public static WolverineOptions UseFabricWorker(
        this WolverineOptions options,
        IConfiguration configuration,
        IServiceProvider? serviceProvider = null);

    public static WolverineOptions UseFabricOutboundLinks(
        this WolverineOptions options,
        IConfiguration configuration,
        IServiceProvider? serviceProvider = null);
}
```

`UseFabricWorker` should:

1. Configure Wolverine service name from `Fabric:Identity:DisplayName` or application name.
2. Configure SQL Server durability if a connection string is supplied.
3. Configure `UseClientToSignalR` to connect to the configured hub.
4. Configure `ToSignalRWithClient` routing for outbound socket messages.

---

## 10. Identity model

### Worker role

Use one role enum for cloud hubs, cloud workers, edge hubs, edge service-host links, edge workers, and installation users.

```csharp
public enum WorkerRole
{
    CloudHub,
    CloudCore,
    CloudLicensing,
    CloudAIAnalysis,
    CloudBotIntegration,

    EdgeHub,
    ServiceHost,
    MilestoneProxy,
    MilestoneLiveEvents,

    InstallationUser
}
```

### Principal

```csharp
public sealed record FabricConnectionPrincipal(
    Guid? InstallationId,
    WorkerRole Role,
    Guid? UserId,
    IReadOnlySet<string> Scopes,
    string DisplayName);
```

Rules:

```text
CloudHub:
  InstallationId should be FabricInstallations.Cloud.
  Joins Cloud().

CloudCore:
  InstallationId should be FabricInstallations.Cloud.
  Joins Cloud() and CloudWorker(CloudCore).

CloudLicensing:
  InstallationId should be FabricInstallations.Cloud.
  Joins Cloud() and CloudWorker(CloudLicensing).

CloudAIAnalysis:
  InstallationId should be FabricInstallations.Cloud.
  Joins Cloud() and CloudWorker(CloudAIAnalysis).

CloudBotIntegration:
  InstallationId should be FabricInstallations.Cloud.
  Joins Cloud() and CloudWorker(CloudBotIntegration).

EdgeHub:
  Must provide InstallationId.
  Hosts local installation traffic.
  When connected outward to cloud, it should use Role=ServiceHost for that outbound connection.

ServiceHost:
  Must provide InstallationId.
  Represents the edge hub as a routable service-host endpoint from cloud/parent infrastructure.

MilestoneProxy:
  Must provide InstallationId.
  Joins Installation(installationId) and InstallationWorker(installationId, MilestoneProxy).

MilestoneLiveEvents:
  Must provide InstallationId.
  Joins Installation(installationId) and InstallationWorker(installationId, MilestoneLiveEvents).

InstallationUser:
  Must provide InstallationId and UserId.
  Joins Installation(installationId) and InstallationUser(installationId, userId).
```

### Removed identity fields

Do not configure, claim, persist, or route by a generated process identifier.

Do not generate these fields:

```text
generated process identifier
process id as public route id
process kind as public route id
cloud worker type
installation worker type
```

The replacement fields are:

```text
fabric.installationId
fabric.role
fabric.userId
fabric.displayName
fabric.scope

X-Fabric-InstallationId
X-Fabric-Role
X-Fabric-UserId
X-Fabric-DisplayName
X-Fabric-Scope
```

### Local development identity

In local development, do not require JWTs.

Support identity from query string values and headers.

SignalR browser/test clients cannot depend on arbitrary headers, so query string values must be supported.

Example generated URL:

```text
http://127.0.0.1:5301/fabric/messages
  ?fabric.installationId=00000000-0000-0000-0000-000000000101
  &fabric.role=MilestoneProxy
  &fabric.displayName=edge-worker-a
```

### JWT identity for non-local-dev

Outside local development, require JWT auth.

Expected claims:

```text
installation_id
role
user_id
scope
name
```

Example edge-hub-to-cloud JWT claims:

```json
{
  "installation_id": "00000000-0000-0000-0000-000000000101",
  "role": "ServiceHost",
  "scope": "fabric.cloud.connect fabric.edge.federate",
  "name": "edge-01"
}
```

The JWT resolver must only trust server-validated claims.

Never trust installation ids, user ids, or roles from message payloads when a verified principal is available from the connection.

---

## 11. Fabric groups and route targets

Public group model:

```csharp
public static class FabricGroups
{
    public static string Installation(Guid installationId)
        => $"installation:{installationId:N}";

    public static string InstallationWorker(Guid installationId, WorkerRole role)
        => $"installation:{installationId:N}:worker:{ToToken(role)}";

    public static string InstallationUser(Guid installationId, Guid userId)
        => $"installation:{installationId:N}:user:{userId:N}";

    public static string ParentInstallation()
        => "parent-installation";

    public static string Cloud()
        => "cloud";

    public static string CloudWorker(WorkerRole role)
        => $"cloud:worker:{ToToken(role)}";

    private static string ToToken<T>(T value) where T : struct, Enum
        => value.ToString().ToKebabCase();
}
```

Expected names:

```text
installation:00000000000000000000000000000101
installation:00000000000000000000000000000101:worker:milestone-proxy
installation:00000000000000000000000000000101:worker:milestone-live-events
installation:00000000000000000000000000000101:worker:service-host
installation:00000000000000000000000000000101:user:00000000000000000000000000030001
parent-installation
cloud
cloud:worker:cloud-core
cloud:worker:cloud-licensing
cloud:worker:cloud-ai-analysis
cloud:worker:cloud-bot-integration
```

Route targets:

```csharp
public abstract record FabricRouteTarget
{
    public sealed record Installation(Guid InstallationId) : FabricRouteTarget;

    public sealed record InstallationWorker(
        Guid InstallationId,
        WorkerRole Role) : FabricRouteTarget;

    public sealed record InstallationUser(
        Guid InstallationId,
        Guid UserId) : FabricRouteTarget;

    public sealed record ParentInstallation : FabricRouteTarget;

    public sealed record Cloud : FabricRouteTarget;

    public sealed record CloudWorker(WorkerRole Role) : FabricRouteTarget;
}
```

---

## 12. Message envelope and result shape

```csharp
public sealed record FabricEnvelope(
    Guid MessageId,
    string MessageKind,
    string ContractName,
    string ContractVersion,
    Guid? SourceInstallationId,
    Guid? TargetInstallationId,
    WorkerRole? SourceRole,
    FabricRouteTarget? Target,
    Guid? SourceUserId,
    string CorrelationId,
    string? CausationId,
    DateTimeOffset SentAtUtc,
    IReadOnlyDictionary<string, string> Headers);
```

The envelope uses installation fields, role, user, route target, correlation, causation, and contract metadata.

The envelope factory must derive source installation/user/role identity from the current connection principal or process identity, not from caller-provided payload fields.

Message interfaces:

```csharp
public interface IFabricMessage
{
    FabricEnvelope Envelope { get; }
}

public interface IFabricCommand : IFabricMessage;
public interface IFabricEvent : IFabricMessage;
public interface IFabricResult : IFabricMessage;

public interface IFabricQuery<TResult> : IFabricMessage;

public interface IFabricSocketMessage : IFabricMessage, WebSocketMessage;
```

Result shape:

```csharp
public sealed record FabricResult<TPayload>(
    bool Success,
    TPayload? Data,
    ProblemDetailsDto? Error);
```

---

## 13. Wolverine + SQL Server durability

Every hub should configure Wolverine durability with SQL Server.

Workers may also configure Wolverine durability when they have a SQL connection string in local/AppHost topology.

Recommended hub setup:

```csharp
builder.Services.AddFabricHub(builder.Configuration);

builder.Host.UseWolverine(opts =>
{
    opts.ServiceName = builder.Configuration["Fabric:Identity:DisplayName"]
        ?? builder.Environment.ApplicationName;

    var connectionString = builder.Configuration.GetConnectionString("Fabric")
        ?? builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Missing SQL Server connection string.");

    opts.UseSqlServerPersistenceAndTransport(
            connectionString,
            schemaName: "wolverine",
            transportSchema: "wolverine_transport")
        .AutoProvision();

    opts.Policies.UseDurableLocalQueues();

    opts.Services.AddDbContextWithWolverineIntegration<FabricDbContext>(x =>
        x.UseSqlServer(connectionString));

    opts.UseEntityFrameworkCoreTransactions();

    opts.UseFabricHub(builder.Configuration);
});
```

Use EF Core transactional Wolverine handlers for workflows that update prototype state and emit follow-up messages.

---

## 14. Redis-backed HybridCache in cloud services

Cloud hub and cloud worker projects must reference Redis from `AddCloudCommonServices(...)`.

Cloud projects should configure:

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("redis")
        ?? builder.Configuration.GetConnectionString("Redis")
        ?? builder.Configuration["ConnectionStrings:redis"]
        ?? builder.Configuration["ConnectionStrings:Redis"];
});

builder.Services.AddHybridCache();
```

Use `HybridCache` for cloud-side cached lookups, including:

```text
installation configuration cache
license status cache
feature definition cache
cloud allowlist cache
bot workspace configuration cache
AI analysis configuration cache
```

HybridCache rules:

```text
Never use Redis as a transport.
Never use Redis as a SignalR backplane in this prototype.
Never store customer snapshots/images in Redis.
Never use Redis as the durable source of truth.
Cache entries must be rebuildable from SQL Server or external service stubs.
```

---

## 15. Fabric support tables

Use EF Core for Fabric support tables where practical.

### `FabricConnectionLease`

Tracks active SignalR connections.

```csharp
public sealed class FabricConnectionLease
{
    public long Id { get; set; }

    public string ConnectionId { get; set; } = default!;
    public Guid? InstallationId { get; set; }
    public WorkerRole Role { get; set; }
    public Guid? UserId { get; set; }

    public string HubProcessName { get; set; } = default!;
    public string DisplayName { get; set; } = default!;

    public DateTimeOffset ConnectedAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }
    public DateTimeOffset? DisconnectedAtUtc { get; set; }
}
```

### `FabricPendingDelivery`

Tracks reliable group or outbound-link deliveries.

```csharp
public sealed class FabricPendingDelivery
{
    public long Id { get; set; }
    public Guid MessageId { get; set; }
    public string MessageType { get; set; } = default!;
    public string TargetGroup { get; set; } = default!;
    public string? OutboundLinkName { get; set; }
    public string PayloadJson { get; set; } = default!;
    public int AttemptCount { get; set; }
    public DateTimeOffset AvailableAtUtc { get; set; }
    public DateTimeOffset? DeliveredAtUtc { get; set; }
    public DateTimeOffset? AcknowledgedAtUtc { get; set; }
    public DateTimeOffset? DeadLetteredAtUtc { get; set; }
    public string? LastError { get; set; }
}
```

### `FabricProcessedMessage`

Deduplicates incoming messages.

```csharp
public sealed class FabricProcessedMessage
{
    public long Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid? SourceInstallationId { get; set; }
    public WorkerRole? SourceRole { get; set; }
    public string ContractName { get; set; } = default!;
    public DateTimeOffset ProcessedAtUtc { get; set; }
}
```

### `FabricStubInvocation`

Audits generated current-code stub handlers.

```csharp
public sealed class FabricStubInvocation
{
    public long Id { get; set; }
    public Guid MessageId { get; set; }
    public string CatalogEntryId { get; set; } = default!;
    public string Context { get; set; } = default!;
    public string MessageKind { get; set; } = default!;
    public string MessageName { get; set; } = default!;
    public string? ResultName { get; set; }
    public string Source { get; set; } = default!;
    public string Destination { get; set; } = default!;
    public string PrototypeOwner { get; set; } = default!;
    public WorkerRole Role { get; set; }
    public DateTimeOffset InvokedAtUtc { get; set; }
}
```

---

## 16. Contract generation

### Contract folders

```text
cloud/contracts
├── Fabric.Cloud.Contracts.csproj
├── CloudContracts.cs
├── ContractCatalog.Cloud.cs
├── ContractMetadata.cs
├── ResultPayloadPlaceholders.cs
├── RequestPayloadPlaceholders.cs
├── ProvisioningAndInstallations
├── LicensingAndActivation
├── AIImageAnalysis
├── BotIntegrations
├── HubSpotLicenseSync
└── PlatformAndHealth

edge/contracts
├── Fabric.Edge.Contracts.csproj
├── Common
├── EdgeContracts.cs
├── ContractCatalog.Edge.cs
├── ContractMetadata.cs
├── ResultPayloadPlaceholders.cs
├── RequestPayloadPlaceholders.cs
├── Recorders
├── HardwareManagement
├── CamerasStreamsAndSnapshots
├── ManagedSites
├── LiveMonitoring
├── DeviceGroups
├── Reporting
├── NotificationsAndEmail
├── MetadataTagsPriorityMaintenance
├── UsersAuthAndAudit
├── Other
└── AsynchronousBackground
```

### Generation rules

For every row in the message catalog:

```text
Command row -> command record + matching result record.
Query row -> query record + matching result record.
Event row -> event record.
Bot RPC command row -> command record + result record.
Async/background row -> command record unless the row explicitly represents an event.
```

Every generated contract must include metadata:

```csharp
public static class ContractMetadata
{
    public const string CatalogEntryId = "...";
    public const string Context = "...";
    public const string MessageName = "...";
    public const string WireType = "...";
    public const string Source = "...";
    public const string Destination = "...";
    public const string MapsFrom = "...";
}
```

Duplicate message names are allowed in the catalog. C# type names must be unique.

Naming rule:

```text
If message name is unique:
  <MessageName>

If message name collides:
  <ContextToken><MessageName><OwnerToken><Ordinal>
```

The Fabric envelope `ContractName` should preserve the catalog message name.

The generated C# type name may be longer to avoid collisions.

---

## 17. Stub handler ownership model

The current application inventory maps to prototype handlers by destination or owning service.

| Catalog destination / owning service | Prototype project      | WorkerRole            | Notes                                                                          |
| ------------------------------------ | ---------------------- | --------------------- | ------------------------------------------------------------------------------ |
| `toolbox-api`                        | `Fabric.Edge.Hub`      | `EdgeHub`             | Tenant-local UI/API and local orchestration surface.                           |
| `milestone-proxy`                    | `Fabric.Edge.WorkerA`  | `MilestoneProxy`      | VMS/Milestone command/query surface.                                           |
| `live-monitor-service`               | `Fabric.Edge.WorkerB`  | `MilestoneLiveEvents` | Camera/recorder state and monitoring events.                                   |
| `toolbox-service-host`               | `Fabric.Edge.WorkerB`  | `ServiceHost`         | Local service-host behaviors and cloud-bound local agent behaviors in the POC. |
| `hardware-library`                   | `Fabric.Edge.WorkerB`  | `ServiceHost`         | Hardware/device background operations in the POC.                              |
| `core-api`                           | `Fabric.Cloud.WorkerA` | `CloudCore`           | Cloud/core installation, provisioning, and cross-installation stubs.           |
| `license-service`                    | `Fabric.Cloud.WorkerB` | `CloudLicensing`      | License generation and validation workflows.                                   |
| `license-portal`                     | `Fabric.Cloud.WorkerB` | `CloudLicensing`      | License portal user/admin workflows.                                           |
| `ai-analysis-api`                    | `Fabric.Cloud.WorkerA` | `CloudAIAnalysis`     | Cloud image-analysis workflows.                                                |
| `slack-bot-web`                      | `Fabric.Cloud.WorkerB` | `CloudBotIntegration` | Bot RPC/event workflows.                                                       |
| `hubspot-sync`                       | `Fabric.Cloud.WorkerA` | `CloudCore`           | HubSpot sync event consumers.                                                  |
| `any authorised caller`              | Route from envelope    | Target-specific       | Stub handler should live with the resolved destination, not the caller.        |

Stub handler requirements:

```text
Compile.
Be deterministic.
Do not call real external systems.
Do not call real Milestone SDK.
Do not store real images/snapshots in cloud services.
Persist FabricStubInvocation audit rows.
Return a valid success result with placeholder payload.
Publish follow-up events where the catalog implies an event should be emitted.
Enforce installation boundaries.
```

Command handler pattern:

```csharp
public static class ActivateLicenseStubHandler
{
    [Transactional]
    public static async Task<ActivateLicenseResult> Handle(
        ActivateLicense command,
        StubHandlerDbContext db,
        CancellationToken cancellationToken)
    {
        db.StubInvocations.Add(FabricStubInvocationFactory.From(command));
        await db.SaveChangesAsync(cancellationToken);

        return ActivateLicenseResult.Success(
            PlaceholderPayloads.Create<LicenseActivationResponse>());
    }
}
```

Query handler pattern:

```csharp
public static class GetInstallationConfigurationStubHandler
{
    public static async Task<GetInstallationConfigurationResult> Handle(
        GetInstallationConfiguration query,
        StubHandlerDbContext db,
        CancellationToken cancellationToken)
    {
        db.StubInvocations.Add(FabricStubInvocationFactory.From(query));
        await db.SaveChangesAsync(cancellationToken);

        return GetInstallationConfigurationResult.Success(
            PlaceholderPayloads.Create<InstallationStatusDto>());
    }
}
```

Event consumer pattern:

```csharp
public static class InstallationRegisteredStubConsumer
{
    public static async Task Handle(
        InstallationRegistered @event,
        StubHandlerDbContext db,
        CancellationToken cancellationToken)
    {
        db.StubInvocations.Add(FabricStubInvocationFactory.From(@event));
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

### Stub owner summary

| Prototype owner        | WorkerRole            | Catalog rows |
| ---------------------- | --------------------- | ------------ |
| `Fabric.Cloud.WorkerA` | `CloudAIAnalysis`     | 44           |
| `Fabric.Cloud.WorkerB` | `CloudBotIntegration` | 33           |
| `Fabric.Cloud.WorkerA` | `CloudCore`           | 16           |
| `Fabric.Cloud.WorkerB` | `CloudLicensing`      | 31           |
| `Fabric.Edge.Hub`      | `EdgeHub`             | 221          |
| `Fabric.Edge.WorkerB`  | `MilestoneLiveEvents` | 30           |
| `Fabric.Edge.WorkerA`  | `MilestoneProxy`      | 28           |
| `Fabric.Edge.WorkerB`  | `ServiceHost`         | 9            |

---

## 18. Project details

### `Fabric.Cloud.Hub`

Purpose:

```text
cloud-side hub and coordination process
accepts cloud workers
accepts edge hubs connected outward as WorkerRole.ServiceHost
supports clustered operation with two hub instances sharing SQL Server
uses Redis-backed HybridCache for cloud-side cached lookups
```

Project shape:

```text
cloud/hub
├── Fabric.Cloud.Hub.csproj
├── Program.cs
├── appsettings.json
├── CloudDbContext.cs
├── CloudEntities.cs
├── Cache
│   ├── CloudHybridCacheRegistration.cs
│   └── CloudCacheKeys.cs
├── Handlers
│   ├── CloudHealthHandlers.cs
│   ├── CloudAlertHandlers.cs
│   └── CloudLicensingHandlers.cs
├── Handlers/CurrentCode
│   ├── CoreApiStubHandlers.cs
│   ├── LicensePortalStubHandlers.cs
│   └── HubSpotStubHandlers.cs
└── Diagnostics
    └── CloudDiagnosticsEndpoints.cs
```

### `Fabric.Cloud.WorkerA`

Purpose:

```text
cloud-side specialized processing
connects to cloud hub with UseClientToSignalR
hosts CloudCore and CloudAIAnalysis stubs
uses HybridCache where useful
```

Default AppHost resource:

```text
cloud-worker-a -> WorkerRole.CloudCore + WorkerRole.CloudAIAnalysis
```

### `Fabric.Cloud.WorkerB`

Purpose:

```text
cloud-side specialized processing
connects to cloud hub with UseClientToSignalR
hosts CloudLicensing and CloudBotIntegration stubs
uses HybridCache where useful
```

Default AppHost resource:

```text
cloud-worker-b -> WorkerRole.CloudLicensing + WorkerRole.CloudBotIntegration
```

### `Fabric.Edge.Hub`

Purpose:

```text
local coordination authority for one edge installation
hosts local Fabric SignalR hub
accepts local edge workers
accepts user/test client connections
uses SQL Server-backed Wolverine durability
uses EF Core for local prototype state
continues to work with cloud disabled or disconnected
optionally connects outward to cloud via UseClientToSignalR
optionally connects outward to parent installation via UseClientToSignalR
hosts toolbox-api tenant-local stubs
```

Project shape:

```text
edge/hub
├── Fabric.Edge.Hub.csproj
├── Program.cs
├── appsettings.json
├── EdgeDbContext.cs
├── EdgeEntities.cs
├── CloudLink
│   ├── EdgeCloudLinkOptions.cs
│   └── CloudBoundMessageMarker.cs
├── ParentLink
│   ├── ParentInstallationLinkOptions.cs
│   └── ParentBoundMessageMarker.cs
├── Handlers
│   ├── EdgeHealthHandlers.cs
│   ├── EdgeAlertHandlers.cs
│   ├── EdgeInstallationSummaryHandlers.cs
│   └── EdgeCloudFederationHandlers.cs
├── Handlers/CurrentCode
│   ├── ToolboxApiStubHandlers.cs
│   └── ManagedSiteStubHandlers.cs
└── Diagnostics
    └── EdgeDiagnosticsEndpoints.cs
```

### `Fabric.Edge.WorkerA`

Purpose:

```text
on-prem/edge worker connectivity
connects to local edge hub using Wolverine SignalR client transport
hosts MilestoneProxy stubs
```

Default AppHost worker for every edge installation:

```text
edge-worker-a -> WorkerRole.MilestoneProxy
```

### `Fabric.Edge.WorkerB`

Purpose:

```text
on-prem/edge worker connectivity
connects to local edge hub using Wolverine SignalR client transport
hosts MilestoneLiveEvents and ServiceHost stubs for the POC
```

Default AppHost worker for every edge installation:

```text
edge-worker-b -> WorkerRole.MilestoneLiveEvents
```

Service-host note:

```text
WorkerRole.ServiceHost is primarily the edge hub's outbound identity when the edge connects to cloud/parent.
For current-code stubs whose owner maps to toolbox-service-host or hardware-library, generate those stubs in Fabric.Edge.WorkerB for this POC unless a later split creates a dedicated edge service-host project.
```

---

## 19. Current application message catalog implementation

The attached current-code inventory is a target-state service bus catalog.

Implementation scope:

```text
Commands + matching results: 190
Queries + matching results: 170
Bot RPC commands: 8
Events: 33
Async/background messages: 11
Total catalog rows: 412
```

### Catalog summary by bounded context

| Bounded context                                                                   | Commands | Queries | Events | Bot RPC | Async/background | Total |
| --------------------------------------------------------------------------------- | -------- | ------- | ------ | ------- | ---------------- | ----- |
| AI Image Analysis                                                                 | 21       | 22      | 3      | 0       | 0                | 46    |
| Asynchronous / background work (today's in-memory queues → bus commands & events) | 0        | 0       | 0      | 0       | 11               | 11    |
| Bot Integrations                                                                  | 8        | 5       | 4      | 0       | 0                | 17    |
| Bot Integrations — RPC commands (Slack bot ⇄ Toolbox)                             | 0        | 0       | 0      | 8       | 0                | 8     |
| Cameras, Streams & Snapshots                                                      | 2        | 9       | 2      | 0       | 0                | 13    |
| Device Groups                                                                     | 10       | 2       | 0      | 0       | 0                | 12    |
| Hardware Management                                                               | 27       | 15      | 2      | 0       | 0                | 44    |
| HubSpot / License Sync                                                            | 0        | 0       | 1      | 0       | 0                | 1     |
| Licensing & Activation                                                            | 13       | 12      | 4      | 0       | 0                | 29    |
| Live Monitoring                                                                   | 5        | 14      | 8      | 0       | 0                | 27    |
| Managed Sites                                                                     | 19       | 10      | 0      | 0       | 0                | 29    |
| Metadata (Tags, Priority, Maintenance)                                            | 22       | 16      | 2      | 0       | 0                | 40    |
| Notifications & Email                                                             | 5        | 2       | 0      | 0       | 0                | 7     |
| Other                                                                             | 15       | 7       | 0      | 0       | 0                | 22    |
| Platform & Health                                                                 | 2        | 8       | 0      | 0       | 0                | 10    |
| Provisioning & Installations                                                      | 12       | 5       | 3      | 0       | 0                | 20    |
| Recorders                                                                         | 3        | 18      | 3      | 0       | 0                | 24    |
| Reporting                                                                         | 8        | 12      | 1      | 0       | 0                | 21    |
| Users, Auth & Audit                                                               | 18       | 13      | 0      | 0       | 0                | 31    |

### Contract and handler generation requirements

For each catalog row:

```text
[ ] Generate a request/event contract.
[ ] Generate a result contract where applicable.
[ ] Generate placeholder request/result payload types when the real type is not available.
[ ] Generate contract metadata.
[ ] Generate a handler or consumer stub in the mapped owner project.
[ ] Persist a FabricStubInvocation row.
[ ] Return deterministic success for commands and queries.
[ ] Avoid real external integrations.
[ ] Enforce installation authorization.
[ ] Add at least one smoke test per bounded context.
```

### Collision handling

The catalog contains repeated message names with different contexts, payloads, destinations, or maps-from values.

Rules:

```text
Preserve original catalog message name in metadata and envelope.
Generate unique C# type names when needed.
Use CatalogEntryId to distinguish rows.
Do not merge rows unless the request shape, result payload, destination, and permissions are semantically identical.
```

---

## 20. Implementation phases

### Phase 1 — Solution scaffolding

Generate solution and all projects with correct references.

Acceptance:

```text
dotnet build succeeds.
Project layout matches the requested cloud/edge/hosting tree.
No legacy service project names exist.
No legacy service registration name exists.
No legacy tenancy public API symbols exist.
```

### Phase 2 — AppHost extension methods

Generate:

```text
AddCloudCommonServices
AddCloudInstance
AddEdgeInstance
CloudCommonServicesResource
CloudInstanceResources
EdgeInstanceResources
CloudInstanceOptions
EdgeInstanceOptions
```

Acceptance:

```text
AppHost has no large repeated project setup blocks.
AddEdgeInstance can be called more than once.
edge-01 and edge-02 both start successfully as identical code instances with different configuration.
edge-worker-a and edge-worker-b display names are used in every edge installation.
```

### Phase 3 — Core abstractions and identity

Generate:

```text
message interfaces
FabricResult<T>
ProblemDetailsDto
FabricEnvelope
WorkerRole
FabricConnectionPrincipal
route targets
FabricGroups
identity options
local-dev identity resolver
JWT identity resolver skeleton
```

Acceptance:

```text
Identity uses installation id + role.
Local-dev identity can be parsed from query string.
JWT identity can be parsed from claims.
No generated process identifier is required for routing.
No separate cloud worker enum exists.
No separate installation worker enum exists.
```

### Phase 4 — Wolverine/SignalR registration

Generate:

```text
AddFabricHub
AddFabricWorker
UseFabricHub
UseFabricWorker
UseFabricOutboundLinks
FabricHub
```

Acceptance:

```text
Cloud and edge hubs expose /fabric/messages.
Workers connect with UseClientToSignalR.
Edge hub can also connect outward to cloud with UseClientToSignalR.
```

### Phase 5 — SQL Server durability and Redis HybridCache

Generate:

```text
FabricDbContext
connection leases
pending deliveries
processed messages
stub invocation auditing
Wolverine SQL Server configuration
EF Core prototype DbContexts
Redis-backed HybridCache registration in cloud services
```

Acceptance:

```text
SQL Server database contains Wolverine tables.
SQL Server database contains Fabric support tables.
Cloud services can resolve HybridCache.
HybridCache uses Redis as distributed backing cache.
Redis is not used as a SignalR backplane.
Redis is not used as the message bus.
Handlers can update EF state and publish follow-up messages transactionally.
```

### Phase 6 — Minimal demo contracts and handlers

Generate small demo contracts and handlers:

```text
RunHealthCheck
HealthCheckAccepted
HealthCheckCompleted
GetInstallationSummary
InstallationSummary
RaiseEdgeAlert
EdgeAlertObservedByCloud
AnalyzeImageHealth
LicenseStatusChanged
```

Acceptance:

```text
RunHealthCheck flows from test client to edge worker.
RaiseEdgeAlert flows from edge to cloud when cloud link is enabled.
Cloud can target InstallationWorker(edgeId, ServiceHost).
```

### Phase 7 — Current-code catalog contracts

Generate all current-code contracts from the catalog.

Acceptance:

```text
190 command contracts compile.
190 matching command result contracts compile.
170 query contracts compile.
170 matching query result contracts compile.
8 bot RPC command/result pairs compile.
33 event contracts compile.
11 async/background contracts compile.
Every generated contract has metadata.
Every generated contract has a deterministic wire type.
```

### Phase 8 — Current-code stub handlers

Generate mapped stubs in cloud hub, cloud worker, edge hub, and edge worker projects.

Acceptance:

```text
Every command has a handler.
Every query has a handler.
Every event has a consumer where the catalog has subscribers.
Every async/background command has a handler.
Every stub writes FabricStubInvocation.
Every command/query returns a success result unless explicitly testing failure behavior.
```

### Phase 9 — Verification

Generate verification clients/tests as needed without adding them to the core project layout.

Acceptance:

```text
All required verification scenarios pass.
Tests use local-dev identity by default.
JWT-mode tests cover missing/invalid/wrong-installation identity.
Catalog stub tests cover every bounded context.
```

---

## 21. Verification plan

Required scenarios:

```text
local single-edge client-to-worker command/result
edge-to-cloud event flow
cloud-to-edge service-host command
cloud disabled/offline edge operation
cloud reconnect drains pending deliveries
duplicate message id is idempotent
two-installation isolation
cloud cluster routing through shared SQL state
JWT mode rejects missing token
JWT mode rejects wrong installation
current-code catalog compiles
current-code stubs are all discoverable by Wolverine
one command/query/event smoke test per bounded context
```

Two-installation isolation:

```text
Start edge-01 hub and edge-02 hub.
Both hubs run the same Fabric.Edge.Hub code.
Start user/test client connected to edge-01.
Attempt command targeting edge-02.
Assert rejected.
Assert no edge-02 handler runs.
```

Cloud cluster routing:

```text
Start cloud-hub-01 and cloud-hub-02 sharing FabricCloud.
Connect edge-01 to cloud-hub-01.
Connect cloud-worker-b to cloud-hub-02.
Send cloud command that requires cloud-worker-b.
Assert SQL-backed dispatch coordinates delivery.
Assert result is visible through either cloud hub.
```

Duplicate message handling:

```text
Send same MessageId twice.
Assert handler side effect occurs once.
Assert duplicate is acknowledged or ignored safely.
```

---

## 22. Local run commands

```bash
dotnet restore

dotnet build

dotnet run --project hosting/app-host/Fabric.AppHost.csproj
```

Manual minimal run without Aspire:

```bash
# terminal 1
dotnet run --project cloud/hub/Fabric.Cloud.Hub.csproj

# terminal 2
dotnet run --project cloud/worker-a/Fabric.Cloud.WorkerA.csproj

# terminal 3
dotnet run --project cloud/worker-b/Fabric.Cloud.WorkerB.csproj

# terminal 4
dotnet run --project edge/hub/Fabric.Edge.Hub.csproj

# terminal 5
dotnet run --project edge/worker-a/Fabric.Edge.WorkerA.csproj

# terminal 6
dotnet run --project edge/worker-b/Fabric.Edge.WorkerB.csproj
```

---

## 23. Non-goals for the prototype

Do not add:

```text
RabbitMQ
NATS
Kafka
Azure SignalR
Redis SignalR backplane
Redis pub/sub as service bus
Kubernetes
real Milestone SDK integration
real production identity provider
real AI image analysis
installer packaging
browser UI project
```

The prototype should stay focused on:

```text
Wolverine
SignalR
SQL Server
EF Core
Redis-backed HybridCache for cloud cache only
Aspire local orchestration
installation-scoped routing
edge-hub outbound cloud connection
current-code message catalog stubs
```

---

## 24. Final acceptance checklist

The generated prototype is acceptable when:

```text
[ ] Solution builds from clean checkout.
[ ] Root layout matches the requested poc-wolverine tree.
[ ] AppHost starts SQL Server 2025 in Docker.
[ ] AppHost starts Redis.
[ ] AddCloudCommonServices exists and is used by AppHost.
[ ] AddCloudInstance exists and is used by AppHost.
[ ] AddEdgeInstance exists and is used by AppHost more than once.
[ ] AppHost starts two cloud hubs.
[ ] AppHost starts cloud-worker-a and cloud-worker-b.
[ ] AppHost starts edge-01 hub.
[ ] AppHost starts edge-02 hub.
[ ] AppHost starts edge-worker-a and edge-worker-b for edge-01.
[ ] AppHost starts edge-worker-a and edge-worker-b for edge-02.
[ ] Edge hubs expose local SignalR hubs.
[ ] Edge hubs connect outward to cloud hubs when enabled.
[ ] Cloud receives edge hub connections as WorkerRole.ServiceHost.
[ ] Workers connect with UseClientToSignalR.
[ ] Local-dev identity works without JWT.
[ ] JWT mode can be enabled and rejects invalid connections.
[ ] WorkerRole is the only worker role enum.
[ ] Public routing uses installation id + WorkerRole.
[ ] FabricGroups public API matches the required group model.
[ ] No legacy tenancy public APIs remain.
[ ] Wolverine durable storage uses SQL Server.
[ ] EF Core is used for prototype state.
[ ] Cloud services use HybridCache backed by Redis.
[ ] Redis is not used as SignalR backplane.
[ ] Client-to-edge health check works.
[ ] Edge-to-cloud alert works.
[ ] Cloud-to-edge service-host command works.
[ ] Offline cloud scenario stores pending delivery and drains after reconnect.
[ ] Duplicate message scenario is idempotent.
[ ] Two-installation isolation is enforced.
[ ] 190 current-code command stubs compile.
[ ] 170 current-code query stubs compile.
[ ] 8 bot RPC command stubs compile.
[ ] 33 event consumers compile.
[ ] 11 async/background stubs compile.
[ ] Every current-code stub records a FabricStubInvocation row.
```

---
# Appendix A — Full current application service-bus catalog


This appendix preserves the current application catalog rows used for contract and stub generation. Project ownership is defined by the mapping rules in section 17. Generated metadata should assign deterministic catalog entry ids from bounded context, message kind, and row ordinal.

> **Purpose.** This catalog defines the message contracts we will use to move the system **off direct API/REST/SOAP calls** and onto a service bus. Each current cross-service capability is re-expressed as a **Command** (with a **Result**), a **Query** (with a **Result**), or an **Event**. It is a design target, not a description of today's HTTP surface.
>
> Derived from static analysis of `the current application source tree` (see `service-bus-inventory.md` for the raw transport evidence each message maps from).

## How to read this document

- **Command** — an instruction to *do something*; routed point-to-point to exactly one owning service; always paired with a **Result** (`<Command>Result`) returned to the sender (success/failure + payload).
- **Query** — a request to *read something*; request/reply to the owning service; paired with a **Result** carrying the projection.
- **Event** — a *fact that already happened*; published once, fanned out to zero-or-more subscribers (pub/sub); past-tense name; no reply.
- **Source** — the app/service that sends the message. **Destination** — the service that owns the handler. **Target destination(s)** — where the reply goes (commands/queries) or which services consume the fact (events).
- **Permissions** — the authorisation a message must carry, mapped from the current policies (see below).

### Summary counts

| Message type                  | Count |
| ----------------------------- | ----: |
| Commands (+ matching Results) |   190 |
| Queries (+ matching Results)  |   170 |
| Bot RPC commands              |     8 |
| Events                        |    33 |

## Message envelope (all messages)

Every message carries a common envelope so the bus can route, correlate, secure and de-duplicate:

| Field                               | Type           | Purpose                                                      |
| ----------------------------------- | -------------- | ------------------------------------------------------------ |
| `MessageId`                         | Guid           | Unique id of this message (idempotency key).                 |
| `CorrelationId`                     | Guid           | Ties a command/query to its result and to downstream events. |
| `CausationId`                       | Guid           | The `MessageId` that caused this message.                    |
| `InstallationId` (`installationId`) | Guid           | Installation scope; mirrors today's `installationId` claim.  |
| `Subject`                           | string         | Acting user/system principal (`sub`).                        |
| `Permissions`                       | string[]       | Caller permission claims (e.g. `cloud.api.*`).               |
| `Application`                       | enum           | Originating app (Toolbox / LicensePortal / Bot / System).    |
| `Timestamp`                         | DateTimeOffset | When the message was produced.                               |
| `ReplyTo`                           | string         | (commands/queries) address the Result is returned to.        |

### Result shape

Every `*Result` is `{ bool Success, TPayload? Data, ProblemDetails? Error }` so failures travel on the bus instead of as HTTP status codes.

## Permission model (mapped from current code)

The current authorisation primitives map onto per-message requirements as follows:

| Today (code)                                                                        | On the bus                                                    |
| ----------------------------------------------------------------------------------- | ------------------------------------------------------------- |
| `permissions` claim incl. `cloud.api.*` (`WellKnownFabricPermissions.CloudApi`)     | Service scope `cloud.api.*` in envelope `Permissions`.        |
| `AllowToolboxInstallations` (active `InstallationId` + `cloud.api.*` + Toolbox app) | **Toolbox installation token**, installation-scoped.          |
| `AllowLicensePortalUsers` (LicensePortal app)                                       | **License Portal user**.                                      |
| `RequireUserType(User/System)` (FabricLocal scheme)                                 | **Toolbox user** (`User`) or **System** (service-to-service). |
| `RequireUserType(ManagedSite)` (managed-site hub)                                   | **ManagedSite user** (federated child site).                  |
| `[AllowAnonymous]` on register/activate/check                                       | **Anonymous** with installation secret / signed license key.  |
| ASMX `GenerateLicense(encryptedJson)`                                               | **Internal/system**, RSA-encrypted payload.                   |

> Permission values below are **derived defaults** from the owning endpoint's policy and should be confirmed per message during implementation.

## Messages by bounded context

### Provisioning & Installations

#### Commands

| Command                           | Result                                  | Shape (request)                                | Result payload                  | Source                     | Destination | Target (reply-to)          | Permissions                                                                      | Maps from                                      |
| --------------------------------- | --------------------------------------- | ---------------------------------------------- | ------------------------------- | -------------------------- | ----------- | -------------------------- | -------------------------------------------------------------------------------- | ---------------------------------------------- |
| `ActivateLicense`                 | `ActivateLicenseResult`                 | LicenseActivationRequest request               | LicenseActivationResponse       | toolbox-api                | core-api    | toolbox-api                | Anonymous — installation registration secret / signed license key                | POST /api/licensing/activate                   |
| `CheckLicense`                    | `CheckLicenseResult`                    | CheckLicenseRequest request                    | CheckLicenseResponse            | toolbox-api                | core-api    | toolbox-api                | Anonymous — installation registration secret / signed license key                | POST /api/licensing/check                      |
| `DeactivateCloud`                 | `DeactivateCloudResult`                 | —                                              | FileResponse                    | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | POST api/cloud/deactivate                      |
| `DeactivateInstallation`          | `DeactivateInstallationResult`          | —                                              | Ack (success/failure)           | any authorised caller      | core-api    | any authorised caller      | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | POST (controller-root)                         |
| `DeactivateInstallation`          | `DeactivateInstallationResult`          | —                                              | Ack (success/failure)           | toolbox-api                | core-api    | toolbox-api                | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | POST /api/installations/deactivate             |
| `DeactivateSitesCloud`            | `DeactivateSitesCloudResult`            | System.Guid siteId                             | FileResponse                    | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | POST api/sites/{siteId}/cloud/deactivate       |
| `RegisterClient`                  | `RegisterClientResult`                  | RegistrationModel model                        | Ack (success/failure)           | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | POST api/license/clients/register              |
| `RegisterInstallation`            | `RegisterInstallationResult`            | RegistrationRequest request                    | RegistrationResponse            | toolbox-api                | core-api    | toolbox-api                | Anonymous — installation registration secret / signed license key                | POST /api/installations/register               |
| `RegisterManagedSiteRegistration` | `RegisterManagedSiteRegistrationResult` | ManagedSiteRegistrationRequest registration    | ManagedSiteRegistrationResponse | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | POST api/sites/register                        |
| `RegisterRegistration`            | `RegisterRegistrationResult`            | RegistrationRequest request                    | RegistrationResponse            | any authorised caller      | core-api    | any authorised caller      | Anonymous — installation registration secret / signed license key                | POST register                                  |
| `UpdateAddresses`                 | `UpdateAddressesResult`                 | InstallationAddressConfigurationUpdate update  | Ack (success/failure)           | any authorised caller      | core-api    | any authorised caller      | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | PUT (controller-root)                          |
| `UpdateInstallationAddresses`     | `UpdateInstallationAddressesResult`     | InstallationAddressConfigurationUpdate request | Ack (success/failure)           | toolbox-api                | core-api    | toolbox-api                | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | PUT /api/installations/configuration/addresses |

#### Queries

| Query                          | Result                               | Shape (request)          | Result payload        | Source                | Destination   | Target (reply-to)     | Permissions                                                                      | Maps from                            |
| ------------------------------ | ------------------------------------ | ------------------------ | --------------------- | --------------------- | ------------- | --------------------- | -------------------------------------------------------------------------------- | ------------------------------------ |
| `CompleteOAuthResponse`        | `CompleteOAuthResponseResult`        | OAuthResponseQuery query | Ack (success/failure) | any authorised caller | slack-bot-web | any authorised caller | TBD                                                                              | GET complete                         |
| `Connect`                      | `ConnectResult`                      | —                        | Ack (success/failure) | any authorised caller | slack-bot-web | any authorised caller | TBD                                                                              | GET (controller-root)                |
| `GetInstallationConfiguration` | `GetInstallationConfigurationResult` | —                        | InstallationStatusDto | any authorised caller | core-api      | any authorised caller | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET (controller-root)                |
| `GetInstallationConfiguration` | `GetInstallationConfigurationResult` | —                        | InstallationStatusDto | toolbox-api           | core-api      | toolbox-api           | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET /api/installations/configuration |
| `GetUserAccess`                | `GetUserAccessResult`                | string userId            | UserAccessDto         | toolbox-api           | core-api      | toolbox-api           | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET /api/users/{userId}/access       |

#### Events

| Event                          | Shape (payload)                                       | Source   | Subscribers (target destinations)          | Permissions                      | Purpose / maps from                            |
| ------------------------------ | ----------------------------------------------------- | -------- | ------------------------------------------ | -------------------------------- | ---------------------------------------------- |
| `InstallationAddressesUpdated` | Guid InstallationId, AddressInfo[] Addresses          | core-api | hubspot-sync                               | Service scope — internal pub/sub | Installation address configuration changed     |
| `InstallationDeactivated`      | Guid InstallationId, DateTimeOffset DeactivatedAt     | core-api | license-service, hubspot-sync, toolbox-api | Service scope — internal pub/sub | Installation was deactivated                   |
| `InstallationRegistered`       | Guid InstallationId, string Name, AddressInfo Address | core-api | license-portal, hubspot-sync, toolbox-api  | Service scope — internal pub/sub | Emitted after a Toolbox installation registers |

### Licensing & Activation

#### Commands

| Command                | Result                       | Shape (request)                                                     | Result payload        | Source                     | Destination     | Target (reply-to)          | Permissions                                                                      | Maps from                                                            |
| ---------------------- | ---------------------------- | ------------------------------------------------------------------- | --------------------- | -------------------------- | --------------- | -------------------------- | -------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| `ActivateLicense`      | `ActivateLicenseResult`      | LicenseActivationRequest activation                                 | Ack (success/failure) | any authorised caller      | core-api        | any authorised caller      | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | POST activate                                                        |
| `ActivateLicense`      | `ActivateLicenseResult`      | LicenseActivationRequest activation                                 | Ack (success/failure) | any authorised caller      | core-api        | any authorised caller      | Anonymous — installation registration secret / signed license key                | POST ~/api/ActivateLicense                                           |
| `ActivateLicense`      | `ActivateLicenseResult`      | LicenseActivationModel activation                                   | FileResponse          | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | PUT api/license/activate                                             |
| `GenerateLicense`      | `GenerateLicenseResult`      | string encryptedJson                                                | string                | license-portal             | license-service | license-portal             | Internal/system — RSA-encrypted payload (no interactive auth)                    | SOAP GenerateLicense                                                 |
| `PerformCheck`         | `PerformCheckResult`         | CheckLicenseRequest model                                           | Ack (success/failure) | any authorised caller      | core-api        | any authorised caller      | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | POST check                                                           |
| `PerformCheck`         | `PerformCheckResult`         | CheckLicenseRequest model                                           | Ack (success/failure) | any authorised caller      | core-api        | any authorised caller      | Anonymous — installation registration secret / signed license key                | POST ~/api/CheckLicense                                              |
| `RemoveLicenseFeature` | `RemoveLicenseFeatureResult` | Guid licenseId, Guid featureId                                      | Ack (success/failure) | license-portal             | core-api        | license-portal             | License Portal user (LicensePortal app)                                          | DELETE /api/mgmt/licenses/{licenseId}/features/{featureId}           |
| `RemoveLicenseFeature` | `RemoveLicenseFeatureResult` | Guid licenseId, Guid featureId                                      | Ack (success/failure) | any authorised caller      | core-api        | any authorised caller      | License Portal user (LicensePortal app)                                          | DELETE {featureId:guid}                                              |
| `ResetFeatureTrial`    | `ResetFeatureTrialResult`    | Guid licenseId, Guid featureId                                      | Ack (success/failure) | license-portal             | core-api        | license-portal             | License Portal user (LicensePortal app)                                          | POST /api/mgmt/licenses/{licenseId}/features/{featureId}/reset-trial |
| `ResetTrial`           | `ResetTrialResult`           | Guid licenseId, Guid featureId                                      | Ack (success/failure) | any authorised caller      | core-api        | any authorised caller      | License Portal user (LicensePortal app)                                          | POST {featureId:guid}/reset-trial                                    |
| `UpdateLicense`        | `UpdateLicenseResult`        | LicenseUpdateModel update                                           | FileResponse          | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | POST api/license/update                                              |
| `UpdateLicenseFeature` | `UpdateLicenseFeatureResult` | Guid licenseId, Guid featureId, UpdateLicenseFeatureRequest request | Ack (success/failure) | license-portal             | core-api        | license-portal             | License Portal user (LicensePortal app)                                          | PUT /api/mgmt/licenses/{licenseId}/features/{featureId}              |
| `UpdateLicenseFeature` | `UpdateLicenseFeatureResult` | Guid licenseId, Guid featureId, UpdateLicenseFeatureRequest request | Ack (success/failure) | any authorised caller      | core-api        | any authorised caller      | License Portal user (LicensePortal app)                                          | PUT {featureId:guid}                                                 |

#### Queries

| Query                    | Result                         | Shape (request)               | Result payload                                   | Source                     | Destination                      | Target (reply-to)          | Permissions                                           | Maps from                                      |
| ------------------------ | ------------------------------ | ----------------------------- | ------------------------------------------------ | -------------------------- | -------------------------------- | -------------------------- | ----------------------------------------------------- | ---------------------------------------------- |
| `CloudActivationQuery`   | `CloudActivationQueryResult`   | (see contract)                | CloudActivationQueryResult                       | toolbox-api (parent site)  | toolbox-api (managed/child site) | toolbox-api (parent site)  | Toolbox **ManagedSite** user (federated child site)   | SignalR /hubs/sites :: Cloud.ActivationQuery   |
| `CloudDeactivationQuery` | `CloudDeactivationQueryResult` | (see contract)                | CloudDeactivationQueryResult                     | toolbox-api (parent site)  | toolbox-api (managed/child site) | toolbox-api (parent site)  | Toolbox **ManagedSite** user (federated child site)   | SignalR /hubs/sites :: Cloud.DeactivationQuery |
| `GetActivationToken`     | `GetActivationTokenResult`     | —                             | LicenseActivationTokenResponse                   | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/license/update                         |
| `GetFeatureDefinitions`  | `GetFeatureDefinitionsResult`  | —                             | FeatureDefinitionsResponse                       | any authorised caller      | core-api                         | any authorised caller      | License Portal user (LicensePortal app)               | GET (controller-root)                          |
| `GetFeatureDefinitions`  | `GetFeatureDefinitionsResult`  | —                             | FeatureDefinitionsResponse                       | license-portal             | core-api                         | license-portal             | License Portal user (LicensePortal app)               | GET /api/mgmt/licenses/features                |
| `GetLicense`             | `GetLicenseResult`             | —                             | LicenseModel                                     | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/license                                |
| `GetLicenseFeatures`     | `GetLicenseFeaturesResult`     | Guid licenseId                | LicenseFeaturesResponse                          | any authorised caller      | core-api                         | any authorised caller      | License Portal user (LicensePortal app)               | GET (controller-root)                          |
| `GetLicenseFeatures`     | `GetLicenseFeaturesResult`     | Guid licenseId                | LicenseFeaturesResponse                          | license-portal             | core-api                         | license-portal             | License Portal user (LicensePortal app)               | GET /api/mgmt/licenses/{licenseId}/features    |
| `GetLicenses`            | `GetLicensesResult`            | LicenseListingRequest request | LicenseListingResponse                           | any authorised caller      | core-api                         | any authorised caller      | License Portal user (LicensePortal app)               | GET (controller-root)                          |
| `GetLicenses`            | `GetLicensesResult`            | LicenseListingRequest request | LicenseListingResponse                           | license-portal             | core-api                         | license-portal             | License Portal user (LicensePortal app)               | GET /api/mgmt/licenses                         |
| `GetMachineInformation`  | `GetMachineInformationResult`  | —                             | LicenseSystemInformation                         | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/license/system                         |
| `GetUnlicensedHardware`  | `GetUnlicensedHardwareResult`  | —                             | System.Collections.Generic.ICollection<Hardware> | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/hardware/unlicensed                    |

#### Events

| Event                   | Shape (payload)                                                               | Source          | Subscribers (target destinations)         | Permissions                                   | Purpose / maps from                         |
| ----------------------- | ----------------------------------------------------------------------------- | --------------- | ----------------------------------------- | --------------------------------------------- | ------------------------------------------- |
| `LicenseActivated`      | Guid InstallationId, Guid LicenseId, string Edition, DateTimeOffset ExpiresAt | core-api        | toolbox-api, license-portal, hubspot-sync | Service scope — internal pub/sub              | A license was activated for an installation |
| `LicenseChecked`        | Guid InstallationId, Guid LicenseId, bool Valid                               | core-api        | toolbox-api                               | Service scope — internal pub/sub              | Result of a license validity check          |
| `LicenseFeatureUpdated` | Guid LicenseId, Guid FeatureId, FeatureState State                            | core-api        | toolbox-api, license-portal               | License Portal admin action → service pub/sub | A license feature was added/updated/removed |
| `LicenseGenerated`      | string LicenseKey, Guid InstallationId                                        | license-service | license-portal                            | Internal/system                               | A signed license file was generated         |

### Recorders

#### Commands

| Command               | Result                      | Shape (request)                     | Result payload                                               | Source                     | Destination     | Target (reply-to)          | Permissions                                           | Maps from                                    |
| --------------------- | --------------------------- | ----------------------------------- | ------------------------------------------------------------ | -------------------------- | --------------- | -------------------------- | ----------------------------------------------------- | -------------------------------------------- |
| `RenameRecorders`     | `RenameRecordersResult`     | RecorderRenameRequest request       | System.Collections.Generic.ICollection<RecorderChangeResult> | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: RenameRecorders    |
| `RenameRecorders`     | `RenameRecordersResult`     | RecorderRenameRequest request       | RecorderRenameResponse                                       | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/recorders/rename                    |
| `StartRetentionCheck` | `StartRetentionCheckResult` | CameraRetentionCheckRequest request | System.Guid                                                  | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/milestone/recorders/retention/check |

#### Queries

| Query                         | Result                              | Shape (request)                                                                                              | Result payload                                                       | Source                     | Destination | Target (reply-to)          | Permissions                                           | Maps from                                                      |
| ----------------------------- | ----------------------------------- | ------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------- | -------------------------- | ----------- | -------------------------- | ----------------------------------------------------- | -------------------------------------------------------------- |
| `GetArchiveStorage`           | `GetArchiveStorageResult`           | —                                                                                                            | RecorderArchiveStorageResponse                                       | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/recorders/storage/archive                              |
| `GetArchiveStorageHistory`    | `GetArchiveStorageHistoryResult`    | System.Guid recorderId, System.DateTime? startDate, System.DateTime? endDate                                 | RecorderArchiveStorageHistoryResponse                                | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/recorders/{recorderId}/storage/archives/history        |
| `GetCameraRetentionHistory`   | `GetCameraRetentionHistoryResult`   | System.Guid cameraId, System.Guid? storageLocationId, System.DateTime? startDate, System.DateTime? endDate   | FileResponse                                                         | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/recorders/storage/retention/cameras/{cameraId}/history |
| `GetCameraStorage`            | `GetCameraStorageResult`            | —                                                                                                            | System.Collections.Generic.ICollection<CameraStorageReport>          | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/milestone/recorders/storage/cameras                    |
| `GetLiveStorage`              | `GetLiveStorageResult`              | —                                                                                                            | RecorderLiveStorageResponse                                          | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/recorders/storage/live                                 |
| `GetLiveStorageHistory`       | `GetLiveStorageHistoryResult`       | System.Guid recorderId, System.DateTime? startDate, System.DateTime? endDate                                 | RecorderLiveStorageHistoryResponse                                   | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/recorders/{recorderId}/storage/live/history            |
| `GetLiveStorageTrends`        | `GetLiveStorageTrendsResult`        | —                                                                                                            | RecorderLiveStorageTrendResponse                                     | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/recorders/storage/live/trends                          |
| `GetRecorderOverview`         | `GetRecorderOverviewResult`         | RecorderOverviewFilter request                                                                               | System.Collections.Generic.ICollection<RecordingServerOverview>      | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/milestone/recorders/overview                          |
| `GetRecorderRetention`        | `GetRecorderRetentionResult`        | —                                                                                                            | FileResponse                                                         | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/recorders/storage/retention                            |
| `GetRecorderRetentionHistory` | `GetRecorderRetentionHistoryResult` | System.Guid recorderId, System.Guid? storageLocationId, System.DateTime? startDate, System.DateTime? endDate | FileResponse                                                         | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/recorders/{recorderId}/storage/retention/history       |
| `GetRecorders`                | `GetRecordersResult`                | —                                                                                                            | System.Collections.Generic.ICollection<GlobalRecorderModel>          | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/global/recorders                                       |
| `GetRecorders`                | `GetRecordersResult`                | —                                                                                                            | System.Collections.Generic.ICollection<Recorder>                     | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/recorders                                              |
| `GetRetentionResults`         | `GetRetentionResultsResult`         | System.Guid taskId                                                                                           | System.Collections.Generic.ICollection<CameraRetentionDetails>       | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/milestone/recorders/retention/check/results/{taskId}   |
| `GetRetentionTrends`          | `GetRetentionTrendsResult`          | —                                                                                                            | RecorderRetentionTrendResponse                                       | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/recorders/storage/retention/trends                     |
| `GetStorageConfigurations`    | `GetStorageConfigurationsResult`    | —                                                                                                            | System.Collections.Generic.ICollection<RecorderStorageConfiguration> | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/recorders/storage/configurations                       |
| `GetStorageDisks`             | `GetStorageDisksResult`             | —                                                                                                            | System.Collections.Generic.ICollection<RecorderDriveSummary>         | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/recorders/storage/disks                                |
| `SearchCameraRetention`       | `SearchCameraRetentionResult`       | CameraRetentionSearchRequest search                                                                          | CameraRetentionResponse                                              | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/recorders/storage/retention/cameras                   |
| `SearchRecorderRetention`     | `SearchRecorderRetentionResult`     | RecorderRetentionSearchRequest search                                                                        | FileResponse                                                         | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/recorders/storage/retention                           |

#### Events

| Event                      | Shape (payload)                                 | Source                             | Subscribers (target destinations)           | Permissions                                      | Purpose / maps from                     |
| -------------------------- | ----------------------------------------------- | ---------------------------------- | ------------------------------------------- | ------------------------------------------------ | --------------------------------------- |
| `RecorderEvent`            | (see contract)                                  | toolbox-api / live-monitor-service | slack-bot-web → posts Slack notifications   | Bot connectivity token (workspace-scoped)        | bot contract `RecorderEvent`            |
| `RecorderRenamed`          | Guid RecorderId, string OldName, string NewName | toolbox-api                        | live-monitor-service, slack-bot-web (state) | Toolbox user action → installation-local pub/sub | A recorder was renamed                  |
| `RecorderStateUpdateEvent` | (see contract)                                  | toolbox-api / live-monitor-service | slack-bot-web → posts Slack notifications   | Bot connectivity token (workspace-scoped)        | bot contract `RecorderStateUpdateEvent` |

### Hardware Management

#### Commands

| Command                   | Result                          | Shape (request)                                                                        | Result payload                                                      | Source                     | Destination     | Target (reply-to)          | Permissions                                           | Maps from                                         |
| ------------------------- | ------------------------------- | -------------------------------------------------------------------------------------- | ------------------------------------------------------------------- | -------------------------- | --------------- | -------------------------- | ----------------------------------------------------- | ------------------------------------------------- |
| `AutoFocus`               | `AutoFocusResult`               | HardwareSelection selection                                                            | HardwareControlResponse                                             | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/control/autofocus               |
| `ChangeHardwareAddresses` | `ChangeHardwareAddressesResult` | HardwareAddressChangeRequest request                                                   | System.Collections.Generic.ICollection<HardwareChangeResult>        | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: ChangeHardwareAddresses |
| `ChangeHardwareAddresses` | `ChangeHardwareAddressesResult` | HardwareAddressChangeRequest request                                                   | HardwareChangeResponse                                              | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/connection/address              |
| `CheckPasswordStrength`   | `CheckPasswordStrengthResult`   | PasswordStrengthRequest request                                                        | System.Collections.Generic.ICollection<PasswordStrengthCheckResult> | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/passwords/strength-check        |
| `DeleteHardware`          | `DeleteHardwareResult`          | HardwareSelection selection                                                            | System.Collections.Generic.ICollection<HardwareChangeResult>        | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: DeleteHardware          |
| `DeleteHardware`          | `DeleteHardwareResult`          | HardwareSelection selection                                                            | HardwareChangeResponse                                              | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/hardware                               |
| `DisableHardware`         | `DisableHardwareResult`         | HardwareSelection selection                                                            | System.Collections.Generic.ICollection<HardwareChangeResult>        | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: DisableHardware         |
| `DisableHardware`         | `DisableHardwareResult`         | HardwareSelection selection                                                            | HardwareChangeResponse                                              | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/disable                         |
| `DisableHardwareDevices`  | `DisableHardwareDevicesResult`  | HardwareDeviceChangeRequest request                                                    | System.Collections.Generic.ICollection<HardwareDeviceChangeResult>  | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: DisableHardwareDevices  |
| `DisableHardwareDevices`  | `DisableHardwareDevicesResult`  | HardwareDeviceSelection selection                                                      | HardwareDeviceChangeResponse                                        | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/devices/disable                 |
| `DisableHttps`            | `DisableHttpsResult`            | HardwareSelection selection                                                            | HardwareChangeResponse                                              | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/connection/https/disable        |
| `EnableHardware`          | `EnableHardwareResult`          | HardwareSelection selection                                                            | System.Collections.Generic.ICollection<HardwareChangeResult>        | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: EnableHardware          |
| `EnableHardware`          | `EnableHardwareResult`          | HardwareSelection selection                                                            | HardwareChangeResponse                                              | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/enable                          |
| `EnableHardwareDevices`   | `EnableHardwareDevicesResult`   | HardwareDeviceChangeRequest request                                                    | System.Collections.Generic.ICollection<HardwareDeviceChangeResult>  | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: EnableHardwareDevices   |
| `EnableHardwareDevices`   | `EnableHardwareDevicesResult`   | HardwareDeviceSelection selection                                                      | HardwareDeviceChangeResponse                                        | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/devices/enable                  |
| `EnableHttps`             | `EnableHttpsResult`             | HardwareSelection selection                                                            | HardwareChangeResponse                                              | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/connection/https/enable         |
| `EnqueueUpdates`          | `EnqueueUpdatesResult`          | System.Collections.Generic.IEnumerable<FirmwareUpdate> updates                         | FileResponse                                                        | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/firmware/updates/enqueue                 |
| `GeneratePasswords`       | `GeneratePasswordsResult`       | HardwarePasswordGenerationRequest request                                              | System.Collections.Generic.ICollection<string>                      | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/passwords/generate              |
| `PerformHealthCheck`      | `PerformHealthCheckResult`      | PasswordHealthCheckRequest request                                                     | Ack (success/failure)                                               | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/passwords/health-check          |
| `Reboot`                  | `RebootResult`                  | HardwareSelection selection                                                            | HardwareControlResponse                                             | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/control/reboot                  |
| `RenameHardware`          | `RenameHardwareResult`          | HardwareRenameRequest request                                                          | System.Collections.Generic.ICollection<HardwareChangeResult>        | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: RenameHardware          |
| `RenameHardware`          | `RenameHardwareResult`          | HardwareRenameRequest request                                                          | HardwareChangeResponse                                              | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/rename                          |
| `RenameHardwareDevices`   | `RenameHardwareDevicesResult`   | HardwareDeviceRenameRequest request                                                    | System.Collections.Generic.ICollection<HardwareDeviceChangeResult>  | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: RenameHardwareDevices   |
| `RenameHardwareDevices`   | `RenameHardwareDevicesResult`   | HardwareDeviceRenameRequest request                                                    | HardwareDeviceChangeResponse                                        | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/devices/rename                  |
| `UpdatePasswords`         | `UpdatePasswordsResult`         | System.Collections.Generic.IEnumerable<HardwarePasswordUpdate> updates                 | System.Collections.Generic.ICollection<HardwareChangeResult>        | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: UpdatePasswords         |
| `UpdatePasswords`         | `UpdatePasswordsResult`         | PasswordUpdateRequest request                                                          | PasswordUpdateResponse                                              | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/passwords/update                |
| `ValidateIEnumerable`     | `ValidateIEnumerableResult`     | System.Collections.Generic.IEnumerable<HardwarePasswordValidationItem> validationItems | HardwarePasswordValidationResult                                    | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/passwords/validate              |

#### Queries

| Query                      | Result                           | Shape (request)                           | Result payload                                                    | Source                     | Destination     | Target (reply-to)          | Permissions                                           | Maps from                                                |
| -------------------------- | -------------------------------- | ----------------------------------------- | ----------------------------------------------------------------- | -------------------------- | --------------- | -------------------------- | ----------------------------------------------------- | -------------------------------------------------------- |
| `CanViewHardwarePasswords` | `CanViewHardwarePasswordsResult` | —                                         | MilestoneUserRole                                                 | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/milestone/user-role/can-view-password            |
| `GetActiveUpdates`         | `GetActiveUpdatesResult`         | —                                         | System.Collections.Generic.ICollection<ActiveUpdate>              | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/firmware/updates                                 |
| `GetDevice`                | `GetDeviceResult`                | DeviceFilter request                      | System.Collections.Generic.ICollection<LegacyHardwareDevice>      | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/milestone/hardware/devices                      |
| `GetEndpointsByAddress`    | `GetEndpointsByAddressResult`    | HardwareConnectionMacAddressFilter filter | System.Collections.Generic.ICollection<HardwareConnectionDetails> | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/bulk/connection-details/by-macaddress  |
| `GetEndpointsById`         | `GetEndpointsByIdResult`         | HardwareConnectionHardwareIdFilter filter | System.Collections.Generic.ICollection<HardwareConnectionDetails> | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/bulk/connection-details/by-id          |
| `GetFirmwareVersions`      | `GetFirmwareVersionsResult`      | —                                         | HardwareFirmwareInformationResponse                               | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/hardware/firmware/versions                       |
| `GetHardware`              | `GetHardwareResult`              | —                                         | System.Collections.Generic.ICollection<GlobalHardwareModel>       | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/global/hardware                                  |
| `GetHardware`              | `GetHardwareResult`              | —                                         | System.Collections.Generic.ICollection<Hardware>                  | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/hardware                                         |
| `GetHardware`              | `GetHardwareResult`              | —                                         | System.Collections.Generic.ICollection<HardwareDevice>            | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/hardware/devices                                 |
| `GetHardwareCapabilities`  | `GetHardwareCapabilitiesResult`  | —                                         | System.Collections.Generic.ICollection<HardwareCapabilityInfo>    | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/hardware/capabilities                            |
| `GetHardwareCredentials`   | `GetHardwareCredentialsResult`   | HardwareSelection selection               | System.Collections.Generic.ICollection<HardwareCredentials>       | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: GetHardwareCredentials         |
| `GetHealthCheckResults`    | `GetHealthCheckResultsResult`    | System.Guid taskId                        | PasswordHealthCheckResponse                                       | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/hardware/passwords/health-check/results/{taskId} |
| `GetLogs`                  | `GetLogsResult`                  | LogQuery query                            | System.Collections.Generic.ICollection<FirmwareUpdateLogEntry>    | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/firmware/updates/logs                            |
| `GetStorageDetails`        | `GetStorageDetailsResult`        | —                                         | System.Collections.Generic.ICollection<HardwareStorageModel>      | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/hardware/storage                                 |
| `GetSystemHardwareDetails` | `GetSystemHardwareDetailsResult` | —                                         | SystemHardwareDetails                                             | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/hardware/system                                  |

#### Events

| Event                      | Shape (payload)                              | Source      | Subscribers (target destinations) | Permissions                                      | Purpose / maps from                  |
| -------------------------- | -------------------------------------------- | ----------- | --------------------------------- | ------------------------------------------------ | ------------------------------------ |
| `HardwareDeleted`          | Guid[] HardwareIds                           | toolbox-api | toolbox-service-host              | Toolbox user action → installation-local pub/sub | Hardware was removed from the system |
| `HardwarePasswordsUpdated` | Guid[] HardwareIds, DateTimeOffset ChangedAt | toolbox-api | toolbox-service-host              | Toolbox user action → installation-local pub/sub | Hardware credentials were rotated    |

### Cameras, Streams & Snapshots

#### Commands

| Command                | Result                       | Shape (request)                                                   | Result payload                     | Source                     | Destination     | Target (reply-to)          | Permissions                                           | Maps from                          |
| ---------------------- | ---------------------------- | ----------------------------------------------------------------- | ---------------------------------- | -------------------------- | --------------- | -------------------------- | ----------------------------------------------------- | ---------------------------------- |
| `Snapshot`             | `SnapshotResult`             | System.Guid cameraId, int? maxWidth, int? maxHeight, int? quality | CameraSnapshotResponse             | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: Snapshot |
| `UpdateStreamSettings` | `UpdateStreamSettingsResult` | CameraStreamSettingsUpdateRequest request                         | CameraStreamSettingsUpdateResponse | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/hardware/streams/settings  |

#### Queries

| Query                      | Result                           | Shape (request)                                                   | Result payload                                                      | Source                     | Destination     | Target (reply-to)          | Permissions                                           | Maps from                                              |
| -------------------------- | -------------------------------- | ----------------------------------------------------------------- | ------------------------------------------------------------------- | -------------------------- | --------------- | -------------------------- | ----------------------------------------------------- | ------------------------------------------------------ |
| `GetCameraRetention`       | `GetCameraRetentionResult`       | CameraRetentionRequest request                                    | System.Collections.Generic.ICollection<CameraRetentionDetails>      | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: GetCameraRetention           |
| `GetCameraRetention`       | `GetCameraRetentionResult`       | System.Guid cameraId                                              | CameraRetentionDto2                                                 | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/hardware/cameras/{cameraId}/retention          |
| `GetCameras`               | `GetCamerasResult`               | —                                                                 | System.Collections.Generic.ICollection<GlobalCameraModel>           | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/global/cameras                                 |
| `GetCameras`               | `GetCamerasResult`               | —                                                                 | System.Collections.Generic.ICollection<Camera>                      | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/hardware/cameras                               |
| `GetFilteredStreams`       | `GetFilteredStreamsResult`       | HardwareFilter filter                                             | System.Collections.Generic.ICollection<HardwareStreamConfiguration> | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/milestone/hardware/streams                    |
| `GetSingleCameraRetention` | `GetSingleCameraRetentionResult` | System.Guid cameraId                                              | CameraRetentionDetails                                              | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: GetSingleCameraRetention     |
| `GetSnapshot`              | `GetSnapshotResult`              | System.Guid deviceId, int? maxWidth, int? maxHeight, int? quality | FileResponse                                                        | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/milestone/hardware/devices/{deviceId}/snapshot |
| `GetStreamDefinitions`     | `GetStreamDefinitionsResult`     | HardwareSelection selection                                       | HardwareStreamResponse                                              | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/streams/definitions                  |
| `GetStreamSettings`        | `GetStreamSettingsResult`        | HardwareSelection selection                                       | CameraStreamSettingsResponse                                        | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/hardware/streams/settings                     |

#### Events

| Event                    | Shape (payload) | Source                             | Subscribers (target destinations)         | Permissions                               | Purpose / maps from                   |
| ------------------------ | --------------- | ---------------------------------- | ----------------------------------------- | ----------------------------------------- | ------------------------------------- |
| `CameraEvent`            | (see contract)  | toolbox-api / live-monitor-service | slack-bot-web → posts Slack notifications | Bot connectivity token (workspace-scoped) | bot contract `CameraEvent`            |
| `CameraStateUpdateEvent` | (see contract)  | toolbox-api / live-monitor-service | slack-bot-web → posts Slack notifications | Bot connectivity token (workspace-scoped) | bot contract `CameraStateUpdateEvent` |

### AI Image Analysis

#### Commands

| Command                                | Result                                       | Shape (request)                                                                                                                                                                                                                                          | Result payload                         | Source                            | Destination     | Target (reply-to)                 | Permissions                                                                      | Maps from                                                                                |
| -------------------------------------- | -------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------- | --------------------------------- | --------------- | --------------------------------- | -------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `CreatePrompt`                         | `CreatePromptResult`                         | PromptCreateRequest request                                                                                                                                                                                                                              | PromptResponse                         | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | POST /api/v1/prompts                                                                     |
| `CreatePrompt`                         | `CreatePromptResult`                         | PromptCreateRequest body                                                                                                                                                                                                                                 | Ack (success/failure)                  | any authorised caller             | ai-analysis-api | any authorised caller             | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | POST api/v1/Prompts                                                                      |
| `DeletePrompt`                         | `DeletePromptResult`                         | Guid id                                                                                                                                                                                                                                                  | Ack (success/failure)                  | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | DELETE /api/v1/prompts/{id}                                                              |
| `DeletePrompt`                         | `DeletePromptResult`                         | Guid id                                                                                                                                                                                                                                                  | Ack (success/failure)                  | any authorised caller             | ai-analysis-api | any authorised caller             | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | DELETE api/v1/Prompts/{id:guid}                                                          |
| `EditCamera`                           | `EditCameraResult`                           | Guid cameraId, CameraEditRequest request                                                                                                                                                                                                                 | Ack (success/failure)                  | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | POST /api/v1/cameras/{cameraId}                                                          |
| `EditCamera`                           | `EditCameraResult`                           | Guid cameraId, CameraEditRequest request                                                                                                                                                                                                                 | Ack (success/failure)                  | any authorised caller             | ai-analysis-api | any authorised caller             | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | POST api/v1/cameras/{cameraId:guid}                                                      |
| `OverrideAnalysisStatus`               | `OverrideAnalysisStatusResult`               | System.Guid cameraId, System.Guid analysisId, AnalysisStatusOverrideRequest request                                                                                                                                                                      | Ack (success/failure)                  | toolbox-api (UI / clients)        | toolbox-api     | toolbox-api (UI / clients)        | Toolbox user (`User` or `System`), installation-local                            | PUT api/cloud/ai/image-analysis/cameras/{cameraId}/analyses/{analysisId}/status-override |
| `OverrideCameraSnapshotAnalysisStatus` | `OverrideCameraSnapshotAnalysisStatusResult` | Guid cameraId, Guid analysisId, AnalysisStatusOverrideRequest request                                                                                                                                                                                    | Ack (success/failure)                  | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | PUT /api/v1/cameras/{cameraId}/snapshot-analysis/{analysisId}/status-override            |
| `OverrideStatus`                       | `OverrideStatusResult`                       | Guid cameraId, Guid analysisId, AnalysisStatusOverrideRequest request, OverrideAnalysisStatusCommand command                                                                                                                                             | Ack (success/failure)                  | any authorised caller             | ai-analysis-api | any authorised caller             | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | PUT api/v1/cameras/{cameraId:guid}/snapshot-analysis/{analysisId:guid}/status-override   |
| `PerformCameraSnapshotMaintenance`     | `PerformCameraSnapshotMaintenanceResult`     | —                                                                                                                                                                                                                                                        | Ack (success/failure)                  | toolbox-api (UI / clients)        | toolbox-api     | toolbox-api (UI / clients)        | Toolbox user (`User` or `System`), installation-local                            | POST api/cloud/ai/image-analysis/maintenance/camera-snapshot                             |
| `ReportCameraSnapshotAnalysisError`    | `ReportCameraSnapshotAnalysisErrorResult`    | Guid cameraId, CameraSnapshotAnalysisErrorReportRequest request                                                                                                                                                                                          | CameraSnapshotAnalysisCreatedResponse  | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | POST /api/v1/cameras/{cameraId}/snapshot-analysis/error                                  |
| `ReportError`                          | `ReportErrorResult`                          | Guid cameraId, CameraSnapshotAnalysisErrorReportRequest request, ReportCameraSnapshotAnalysisErrorCommand command                                                                                                                                        | Ack (success/failure)                  | any authorised caller             | ai-analysis-api | any authorised caller             | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | POST api/v1/cameras/{cameraId:guid}/snapshot-analysis/error                              |
| `SaveImageAnalysis`                    | `SaveImageAnalysisResult`                    | ImageAnalysisConfigurationDto dto                                                                                                                                                                                                                        | Ack (success/failure)                  | toolbox-api (UI / clients)        | toolbox-api     | toolbox-api (UI / clients)        | Toolbox user (`User` or `System`), installation-local                            | PUT api/cloud/ai/image-analysis/configuration                                            |
| `SetCameraReferenceSnapshot`           | `SetCameraReferenceSnapshotResult`           | Guid cameraId, SetReferenceImageRequest request                                                                                                                                                                                                          | Ack (success/failure)                  | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | PUT /api/v1/cameras/{cameraId}/reference-snapshot                                        |
| `SetReferenceSnapshot`                 | `SetReferenceSnapshotResult`                 | Guid cameraId, SetReferenceImageRequest request                                                                                                                                                                                                          | Ack (success/failure)                  | any authorised caller             | ai-analysis-api | any authorised caller             | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | PUT api/v1/cameras/{cameraId:guid}/reference-image                                       |
| `SetReferenceSnapshot`                 | `SetReferenceSnapshotResult`                 | Guid cameraId, SetReferenceImageRequest request                                                                                                                                                                                                          | Ack (success/failure)                  | any authorised caller             | ai-analysis-api | any authorised caller             | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | PUT api/v1/cameras/{cameraId:guid}/reference-snapshot                                    |
| `SetReferenceSnapshot`                 | `SetReferenceSnapshotResult`                 | System.Guid cameraId, SetReferenceImageRequest request                                                                                                                                                                                                   | Ack (success/failure)                  | toolbox-api (UI / clients)        | toolbox-api     | toolbox-api (UI / clients)        | Toolbox user (`User` or `System`), installation-local                            | PUT api/cloud/ai/image-analysis/cameras/{cameraId}/reference-snapshot                    |
| `StartCameraSnapshotAnalysis`          | `StartCameraSnapshotAnalysisResult`          | Guid cameraId, StreamPart snapshot, Guid recorderId, Guid hardwareId, Guid snapshotId, ImageAnalysisTimeOfDay timeOfDay, StreamPart? referenceSnapshot, Guid? referenceSnapshotId, Guid? referenceAnalysisId, DateTimeOffset? capturedAt, Guid? promptId | CameraSnapshotAnalysisAcceptedResponse | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | POST /api/v1/cameras/{cameraId}/snapshot-analysis                                        |
| `StartCameraSnapshotAnalysis`          | `StartCameraSnapshotAnalysisResult`          | System.Guid cameraId, StartCameraSnapshotAnalysisRequest request                                                                                                                                                                                         | CameraSnapshotAnalysisAcceptedResponse | toolbox-api (UI / clients)        | toolbox-api     | toolbox-api (UI / clients)        | Toolbox user (`User` or `System`), installation-local                            | POST api/cloud/ai/image-analysis/cameras/{cameraId}/snapshot-analysis                    |
| `UpdatePrompt`                         | `UpdatePromptResult`                         | Guid id, PromptEditRequest request                                                                                                                                                                                                                       | PromptResponse                         | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | PUT /api/v1/prompts/{id}                                                                 |
| `UpdatePrompt`                         | `UpdatePromptResult`                         | Guid id, PromptEditRequest body                                                                                                                                                                                                                          | Ack (success/failure)                  | any authorised caller             | ai-analysis-api | any authorised caller             | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | PUT api/v1/Prompts/{id:guid}                                                             |

#### Queries

| Query                                     | Result                                          | Shape (request)                                                                        | Result payload                                                              | Source                            | Destination     | Target (reply-to)                 | Permissions                                                                      | Maps from                                                                 |
| ----------------------------------------- | ----------------------------------------------- | -------------------------------------------------------------------------------------- | --------------------------------------------------------------------------- | --------------------------------- | --------------- | --------------------------------- | -------------------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| `CheckLiveliness`                         | `CheckLivelinessResult`                         | —                                                                                      | string                                                                      | monitoring/infra                  | ai-analysis-api | monitoring/infra                  | Anonymous (liveness/readiness)                                                   | GET /health/live                                                          |
| `CheckReadiness`                          | `CheckReadinessResult`                          | —                                                                                      | HealthReadinessResponse                                                     | monitoring/infra                  | ai-analysis-api | monitoring/infra                  | Anonymous (liveness/readiness)                                                   | GET /health/ready                                                         |
| `GetCamera`                               | `GetCameraResult`                               | Guid cameraId                                                                          | CameraResponse                                                              | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET /api/v1/cameras/{cameraId}                                            |
| `GetCamera`                               | `GetCameraResult`                               | Guid cameraId                                                                          | Ack (success/failure)                                                       | any authorised caller             | ai-analysis-api | any authorised caller             | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET api/v1/cameras/{cameraId:guid}                                        |
| `GetCameraSnapshotAnalysis`               | `GetCameraSnapshotAnalysisResult`               | Guid cameraId, Guid analysisId                                                         | CameraSnapshotAnalysisResponse                                              | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET /api/v1/cameras/{cameraId}/snapshot-analysis/{analysisId}             |
| `GetCameraSnapshotAnalysis`               | `GetCameraSnapshotAnalysisResult`               | System.Guid cameraId, System.Guid analysisId                                           | ImageAnalysisDetailResponse                                                 | toolbox-api (UI / clients)        | toolbox-api     | toolbox-api (UI / clients)        | Toolbox user (`User` or `System`), installation-local                            | GET api/cloud/ai/image-analysis/cameras/{cameraId}/analyses/{analysisId}  |
| `GetCameraSnapshotAnalysisHistory`        | `GetCameraSnapshotAnalysisHistoryResult`        | Guid cameraId, ImageAnalysisHistoryFilters? filters                                    | ImageAnalysisResponse[]                                                     | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET /api/v1/cameras/{cameraId}/snapshot-analysis/history                  |
| `GetCameras`                              | `GetCamerasResult`                              | —                                                                                      | CameraListingResponse[]                                                     | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET /api/v1/cameras                                                       |
| `GetCamerasSnapshotAnalysis`              | `GetCamerasSnapshotAnalysisResult`              | Guid cameraId, Guid analysisId                                                         | Ack (success/failure)                                                       | any authorised caller             | ai-analysis-api | any authorised caller             | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET api/v1/cameras/{cameraId:guid}/snapshot-analysis/{analysisId:guid}    |
| `GetImageAnalysesHistoryByCamera`         | `GetImageAnalysesHistoryByCameraResult`         | System.Guid cameraId, int? daysLimit, int? rowLimit, ImageAnalysisTimeOfDay? timeOfDay | System.Collections.Generic.ICollection<ImageAnalysisHistoryListingResponse> | toolbox-api (UI / clients)        | toolbox-api     | toolbox-api (UI / clients)        | Toolbox user (`User` or `System`), installation-local                            | GET api/cloud/ai/image-analysis/cameras/{cameraId}/history                |
| `GetImageAnalysisCameras`                 | `GetImageAnalysisCamerasResult`                 | System.Guid cameraId                                                                   | CameraResponse                                                              | toolbox-api (UI / clients)        | toolbox-api     | toolbox-api (UI / clients)        | Toolbox user (`User` or `System`), installation-local                            | GET api/cloud/ai/image-analysis/cameras/{cameraId}                        |
| `GetImageAnalysisConfiguration`           | `GetImageAnalysisConfigurationResult`           | —                                                                                      | ImageAnalysisConfigurationDto                                               | toolbox-api (UI / clients)        | toolbox-api     | toolbox-api (UI / clients)        | Toolbox user (`User` or `System`), installation-local                            | GET api/cloud/ai/image-analysis/configuration                             |
| `GetLatestCameraSnapshotAnalysisListings` | `GetLatestCameraSnapshotAnalysisListingsResult` | ImageAnalysisLatestFilters? filters                                                    | ImageAnalysisResponse[]                                                     | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET /api/v1/snapshot-analysis/latest                                      |
| `GetPrompt`                               | `GetPromptResult`                               | Guid id                                                                                | PromptResponse                                                              | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET /api/v1/prompts/{id}                                                  |
| `GetPrompt`                               | `GetPromptResult`                               | Guid id                                                                                | PromptResponse                                                              | any authorised caller             | ai-analysis-api | any authorised caller             | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET api/v1/Prompts/{id:guid}                                              |
| `GetPrompts`                              | `GetPromptsResult`                              | —                                                                                      | PromptResponse[]                                                            | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET /api/v1/prompts                                                       |
| `GetProtectedReferenceSnapshotIds`        | `GetProtectedReferenceSnapshotIdsResult`        | ProtectedReferenceSnapshotIdsRequest filters                                           | ProtectedReferenceSnapshotIdsResponse                                       | toolbox-api, toolbox-service-host | ai-analysis-api | toolbox-api, toolbox-service-host | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET /api/v1/retention/protected-reference-snapshot-ids                    |
| `GetSchedules`                            | `GetSchedulesResult`                            | —                                                                                      | System.Collections.Generic.ICollection<ImageAnalysisScheduledRunDto>        | toolbox-api (UI / clients)        | toolbox-api     | toolbox-api (UI / clients)        | Toolbox user (`User` or `System`), installation-local                            | GET api/cloud/ai/image-analysis/schedules                                 |
| `GetSnapshot`                             | `GetSnapshotResult`                             | System.Guid cameraId, System.Guid snapshotId                                           | Ack (success/failure)                                                       | toolbox-api (UI / clients)        | toolbox-api     | toolbox-api (UI / clients)        | Toolbox user (`User` or `System`), installation-local                            | GET api/cloud/ai/image-analysis/cameras/{cameraId}/snapshots/{snapshotId} |
| `IsHealthy`                               | `IsHealthyResult`                               | —                                                                                      | bool                                                                        | toolbox-api (UI / clients)        | toolbox-api     | toolbox-api (UI / clients)        | Toolbox user (`User` or `System`), installation-local                            | GET api/cloud/ai/image-analysis/healthy                                   |
| `ListImageAnalysisCameras`                | `ListImageAnalysisCamerasResult`                | —                                                                                      | System.Collections.Generic.ICollection<CameraListingResponse>               | toolbox-api (UI / clients)        | toolbox-api     | toolbox-api (UI / clients)        | Toolbox user (`User` or `System`), installation-local                            | GET api/cloud/ai/image-analysis/cameras                                   |
| `ListLatest`                              | `ListLatestResult`                              | —                                                                                      | System.Collections.Generic.ICollection<ImageAnalysisResponse>               | toolbox-api (UI / clients)        | toolbox-api     | toolbox-api (UI / clients)        | Toolbox user (`User` or `System`), installation-local                            | GET api/cloud/ai/image-analysis/results                                   |

#### Events

| Event                             | Shape (payload)                                                                            | Source          | Subscribers (target destinations)                                      | Permissions                                        | Purpose / maps from                                |
| --------------------------------- | ------------------------------------------------------------------------------------------ | --------------- | ---------------------------------------------------------------------- | -------------------------------------------------- | -------------------------------------------------- |
| `CameraSnapshotAnalysisCompleted` | Guid InstallationId, Guid CameraId, Guid AnalysisId, AnalysisStatus Status, string Summary | ai-analysis-api | toolbox-service-host, toolbox-api                                      | Service scope — `cloud.api.*`, installation-scoped | An image analysis finished (success or escalation) |
| `CameraSnapshotAnalysisFailed`    | Guid InstallationId, Guid CameraId, Guid AnalysisId, string Error                          | ai-analysis-api | toolbox-service-host                                                   | Service scope — `cloud.api.*`, installation-scoped | Image analysis failed and was recorded             |
| `ImageAnalysisConfigurationSaved` | — (no payload; signal only)                                                                | toolbox-api     | toolbox-service-host → triggers immediate image-analysis schedule sync | Service event channel — Toolbox **System** scope   | existing `IServiceEvent` on /hubs/service-events   |

### Managed Sites

#### Commands

| Command                          | Result                           | Shape (request)                                                | Result payload                         | Source                     | Destination                      | Target (reply-to)          | Permissions                                           | Maps from                                                      |
| -------------------------------- | -------------------------------- | -------------------------------------------------------------- | -------------------------------------- | -------------------------- | -------------------------------- | -------------------------- | ----------------------------------------------------- | -------------------------------------------------------------- |
| `ActivateCloudActivation`        | `ActivateCloudActivationResult`  | System.Guid siteId, CloudActivationConfiguration configuration | FileResponse                           | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/sites/{siteId}/cloud/activate                         |
| `Approve`                        | `ApproveResult`                  | System.Guid siteId, ManagedSiteApprovalRequest approval        | Ack (success/failure)                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/sites/{siteId}/approve                                |
| `Authenticate`                   | `AuthenticateResult`             | System.Guid siteId, string authenticationToken                 | ManagedSiteAuthenticationResponse      | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/sites/{siteId}/authenticate                           |
| `DeleteLocalSite`                | `DeleteLocalSiteResult`          | —                                                              | Ack (success/failure)                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/sites/local                                         |
| `DeleteSite`                     | `DeleteSiteResult`               | System.Guid siteId                                             | Ack (success/failure)                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/sites/{siteId}                                      |
| `Deny`                           | `DenyResult`                     | System.Guid siteId                                             | Ack (success/failure)                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/sites/{siteId}/deny                                   |
| `RegisterParent`                 | `RegisterParentResult`           | ParentSiteRegistrationRequest registration                     | Ack (success/failure)                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/sites/parent                                           |
| `RemoveConfiguration`            | `RemoveConfigurationResult`      | System.Guid siteId                                             | Ack (success/failure)                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/sites/{siteId}/smtp-configuration                   |
| `RemoveLocalConfiguration`       | `RemoveLocalConfigurationResult` | —                                                              | Ack (success/failure)                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/sites/local/smtp-configuration                      |
| `RemoveRegistration`             | `RemoveRegistrationResult`       | —                                                              | ParentSiteRegistrationRemoveResponse   | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/sites/parent                                        |
| `RemoveRegistration`             | `RemoveRegistrationResult`       | System.Guid siteId, string authenticationToken                 | Ack (success/failure)                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/sites/{siteId}/registration                         |
| `SaveConfiguration`              | `SaveConfigurationResult`        | System.Guid siteId, SiteConfiguration update                   | Ack (success/failure)                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/sites/{siteId}/configuration                           |
| `SaveLocalConfiguration`         | `SaveLocalConfigurationResult`   | SiteConfiguration update                                       | Ack (success/failure)                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/sites/local/configuration                              |
| `SiteConfigurationUpdateCommand` | `Ack (success/failure)`          | (see contract)                                                 | Ack (success/failure)                  | toolbox-api (parent site)  | toolbox-api (managed/child site) | toolbox-api (parent site)  | Toolbox **ManagedSite** user (federated child site)   | SignalR /hubs/sites :: Sites.Configuration.ConfigurationUpdate |
| `SiteDeleteCommand`              | `Ack (success/failure)`          | (see contract)                                                 | Ack (success/failure)                  | toolbox-api (parent site)  | toolbox-api (managed/child site) | toolbox-api (parent site)  | Toolbox **ManagedSite** user (federated child site)   | SignalR /hubs/sites :: Sites.Site.Delete                       |
| `SiteStatusUpdateCommand`        | `Ack (success/failure)`          | (see contract)                                                 | Ack (success/failure)                  | toolbox-api (parent site)  | toolbox-api (managed/child site) | toolbox-api (parent site)  | Toolbox **ManagedSite** user (federated child site)   | SignalR /hubs/sites :: Sites.StatusUpdateCommand               |
| `TestCloudConnection`            | `TestCloudConnectionResult`      | System.Guid siteId                                             | ManagedSiteCloudConnectionTestResponse | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/sites/{siteId}/cloud/connection/test                  |
| `UpdateConfiguration`            | `UpdateConfigurationResult`      | System.Guid siteId, SmtpConfiguration update                   | Ack (success/failure)                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/sites/{siteId}/smtp-configuration                      |
| `UpdateLocalConfiguration`       | `UpdateLocalConfigurationResult` | SmtpConfiguration update                                       | Ack (success/failure)                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/sites/local/smtp-configuration                         |

#### Queries

| Query                   | Result                        | Shape (request)                                | Result payload                                      | Source                     | Destination                      | Target (reply-to)          | Permissions                                           | Maps from                                  |
| ----------------------- | ----------------------------- | ---------------------------------------------- | --------------------------------------------------- | -------------------------- | -------------------------------- | -------------------------- | ----------------------------------------------------- | ------------------------------------------ |
| `ApiProxyQuery`         | `ApiProxyQueryResult`         | (see contract)                                 | ApiProxyQueryResult                                 | toolbox-api (parent site)  | toolbox-api (managed/child site) | toolbox-api (parent site)  | Toolbox **ManagedSite** user (federated child site)   | SignalR /hubs/sites :: Sites.ApiProxyQuery |
| `GetConfiguration`      | `GetConfigurationResult`      | System.Guid siteId                             | SiteConfiguration                                   | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/sites/{siteId}/configuration       |
| `GetConfiguration`      | `GetConfigurationResult`      | System.Guid siteId                             | SmtpConfiguration                                   | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/sites/{siteId}/smtp-configuration  |
| `GetLocalConfiguration` | `GetLocalConfigurationResult` | —                                              | SiteConfiguration                                   | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/sites/local/configuration          |
| `GetLocalConfiguration` | `GetLocalConfigurationResult` | —                                              | SmtpConfiguration                                   | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/sites/local/smtp-configuration     |
| `GetLocalSite`          | `GetLocalSiteResult`          | —                                              | LocalSiteInformation                                | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/sites/local                        |
| `GetNetworkInformation` | `GetNetworkInformationResult` | System.Guid siteId                             | NetworkInformation                                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/sites/{siteId}/system/network      |
| `GetRegistrationStatus` | `GetRegistrationStatusResult` | —                                              | ParentSiteResponse                                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/sites/parent                       |
| `GetRegistrationStatus` | `GetRegistrationStatusResult` | System.Guid siteId, string authenticationToken | ManagedSiteRegistrationStatusResponse               | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/sites/{siteId}/registration        |
| `GetSites`              | `GetSitesResult`              | —                                              | System.Collections.Generic.ICollection<SiteListing> | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/sites                              |

### Live Monitoring

#### Commands

| Command                    | Result                           | Shape (request)                                                       | Result payload        | Source                     | Destination   | Target (reply-to)          | Permissions                                           | Maps from                                        |
| -------------------------- | -------------------------------- | --------------------------------------------------------------------- | --------------------- | -------------------------- | ------------- | -------------------------- | ----------------------------------------------------- | ------------------------------------------------ |
| `CameraStateUpdate`        | `CameraStateUpdateResult`        | CameraStateUpdateEvent cameraStateUpdateEvent                         | Ack (success/failure) | any authorised caller      | slack-bot-web | any authorised caller      | TBD                                                   | POST (controller-root)                           |
| `RecorderStateUpdate`      | `RecorderStateUpdateResult`      | RecorderStateUpdateEvent recorderStateUpdateEvent                     | Ack (success/failure) | any authorised caller      | slack-bot-web | any authorised caller      | TBD                                                   | POST (controller-root)                           |
| `ReportEvents`             | `ReportEventsResult`             | System.Collections.Generic.IEnumerable<LiveMonitoringEventLog> events | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api   | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/live-monitoring/events                  |
| `SendCameraNotification`   | `SendCameraNotificationResult`   | CameraNotification notification                                       | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api   | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/live-monitoring/notifications/cameras   |
| `SendRecorderNotification` | `SendRecorderNotificationResult` | RecorderNotification notification                                     | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api   | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/live-monitoring/notifications/recorders |

#### Queries

| Query                        | Result                             | Shape (request)                                                                                           | Result payload                                                   | Source                     | Destination                      | Target (reply-to)          | Permissions                                           | Maps from                                                      |
| ---------------------------- | ---------------------------------- | --------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------- | -------------------------- | -------------------------------- | -------------------------- | ----------------------------------------------------- | -------------------------------------------------------------- |
| `GetCameraEvents`            | `GetCameraEventsResult`            | System.Guid cameraId                                                                                      | System.Collections.Generic.ICollection<LiveMonitoringEventModel> | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/live-monitoring/events/cameras/{cameraId}              |
| `GetCameraUptimeHistory`     | `GetCameraUptimeHistoryResult`     | System.Guid cameraId, System.DateTime? startDate, System.DateTime? endDate                                | CameraUptimeHistoryResponse                                      | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/live-monitoring/cameras/{cameraId}/uptime/history      |
| `GetCameraUptimes`           | `GetCameraUptimesResult`           | —                                                                                                         | CameraUptimeTrendResponse                                        | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/live-monitoring/cameras/uptime/trends                  |
| `GetCurrentState`            | `GetCurrentStateResult`            | —                                                                                                         | LiveMonitoringStatusResponse                                     | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/live-monitoring/status                                 |
| `GetEvents`                  | `GetEventsResult`                  | int? count, long? after, long? before, System.DateTime? since, LiveMonitoringSourceTypeFilter? sourceType | System.Collections.Generic.ICollection<LiveMonitoringEventModel> | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/live-monitoring/events                                 |
| `GetLiveMonitoringEvents`    | `GetLiveMonitoringEventsResult`    | GlobalLiveMonitoringEventRequest request                                                                  | System.Collections.Generic.ICollection<LiveMonitoringEventModel> | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/global/live-monitoring/events/search                  |
| `GetLiveMonitoringStatus`    | `GetLiveMonitoringStatusResult`    | GlobalLiveMonitoringStatusRequest request                                                                 | GlobalLiveMonitoringStatusResponse                               | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/global/live-monitoring/status                         |
| `GetRecorderEvents`          | `GetRecorderEventsResult`          | System.Guid recorderId                                                                                    | System.Collections.Generic.ICollection<LiveMonitoringEventModel> | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/live-monitoring/events/recorders/{recorderId}          |
| `GetRecorderUptimeAverages`  | `GetRecorderUptimeAveragesResult`  | System.DateTime? startDate, System.DateTime? endDate                                                      | RecorderUptimeAverageResponse                                    | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/live-monitoring/recorders/uptime/averages              |
| `GetRecorderUptimeHistory`   | `GetRecorderUptimeHistoryResult`   | System.Guid recorderId, System.DateTime? startDate, System.DateTime? endDate                              | RecorderUptimeHistoryResponse                                    | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/live-monitoring/recorders/{recorderId}/uptime/history  |
| `GetRecorderUptimes`         | `GetRecorderUptimesResult`         | —                                                                                                         | RecorderUptimeTrendResponse                                      | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/live-monitoring/recorders/uptime/trends                |
| `GetStatistics`              | `GetStatisticsResult`              | —                                                                                                         | LiveMonitoringStatistics                                         | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/live-monitoring/statistics                             |
| `LiveMonitoringEventQuery`   | `LiveMonitoringEventQueryResult`   | (see contract)                                                                                            | LiveMonitoringEventQueryResult                                   | toolbox-api (parent site)  | toolbox-api (managed/child site) | toolbox-api (parent site)  | Toolbox **ManagedSite** user (federated child site)   | SignalR /hubs/sites :: LiveMonitoring.LiveMonitoringEventQuery |
| `SearchCameraUptimeAverages` | `SearchCameraUptimeAveragesResult` | CameraUptimeAverageRequest filter                                                                         | FileResponse                                                     | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/live-monitoring/cameras/uptime/averages               |

#### Events

| Event                            | Shape (payload)                                         | Source                             | Subscribers (target destinations)         | Permissions                               | Purpose / maps from                                |
| -------------------------------- | ------------------------------------------------------- | ---------------------------------- | ----------------------------------------- | ----------------------------------------- | -------------------------------------------------- |
| `CameraStateChanged`             | Guid CameraId, CameraState State, DateTimeOffset At     | live-monitor-service               | toolbox-api, slack-bot-web                | System scope (monitoring pipeline)        | A monitored camera changed state (video loss etc.) |
| `Event`                          | (see contract)                                          | toolbox-api / live-monitor-service | slack-bot-web → posts Slack notifications | Bot connectivity token (workspace-scoped) | bot contract `Event`                               |
| `EventLogEvent`                  | (see contract)                                          | toolbox-api / live-monitor-service | slack-bot-web → posts Slack notifications | Bot connectivity token (workspace-scoped) | bot contract `EventLogEvent`                       |
| `LiveMonitoringCameraEvent`      | (see contract)                                          | toolbox-api / live-monitor-service | slack-bot-web → posts Slack notifications | Bot connectivity token (workspace-scoped) | bot contract `LiveMonitoringCameraEvent`           |
| `LiveMonitoringEvent`            | (see contract)                                          | toolbox-api / live-monitor-service | slack-bot-web → posts Slack notifications | Bot connectivity token (workspace-scoped) | bot contract `LiveMonitoringEvent`                 |
| `LiveMonitoringRecorderEvent`    | (see contract)                                          | toolbox-api / live-monitor-service | slack-bot-web → posts Slack notifications | Bot connectivity token (workspace-scoped) | bot contract `LiveMonitoringRecorderEvent`         |
| `LiveMonitoringStateChangeEvent` | (see contract)                                          | toolbox-api / live-monitor-service | slack-bot-web → posts Slack notifications | Bot connectivity token (workspace-scoped) | bot contract `LiveMonitoringStateChangeEvent`      |
| `RecorderStateChanged`           | Guid RecorderId, RecorderState State, DateTimeOffset At | live-monitor-service               | toolbox-api, slack-bot-web                | System scope (monitoring pipeline)        | A monitored recorder changed state                 |

### Device Groups

#### Commands

| Command                    | Result                           | Shape (request)                                                            | Result payload                                                        | Source                     | Destination     | Target (reply-to)          | Permissions                                           | Maps from                                          |
| -------------------------- | -------------------------------- | -------------------------------------------------------------------------- | --------------------------------------------------------------------- | -------------------------- | --------------- | -------------------------- | ----------------------------------------------------- | -------------------------------------------------- |
| `AddDeviceGroupMembers`    | `AddDeviceGroupMembersResult`    | DeviceGroupMemberChangeRequest request                                     | System.Collections.Generic.ICollection<DeviceGroupMemberChangeResult> | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: AddDeviceGroupMembers    |
| `AddMembers`               | `AddMembersResult`               | System.Collections.Generic.IEnumerable<DeviceGroupMemberSelection> updates | Ack (success/failure)                                                 | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/device-groups/members/bulk/add            |
| `CreateDeviceGroup`        | `CreateDeviceGroupResult`        | CreateDeviceGroupRequest request                                           | CreateDeviceGroupResponse                                             | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: CreateDeviceGroup        |
| `CreateGroup`              | `CreateGroupResult`              | CreateDeviceGroupRequest request                                           | CreateDeviceGroupResponse                                             | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/device-groups                             |
| `DeleteDeviceGroups`       | `DeleteDeviceGroupsResult`       | DeviceGroupSelection selection                                             | System.Collections.Generic.ICollection<DeviceGroupChangeResult>       | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: DeleteDeviceGroups       |
| `DeleteGroups`             | `DeleteGroupsResult`             | System.Collections.Generic.IEnumerable<System.Guid> deviceGroupIds         | Ack (success/failure)                                                 | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/device-groups/bulk/delete                 |
| `RemoveDeviceGroupMembers` | `RemoveDeviceGroupMembersResult` | DeviceGroupMemberChangeRequest request                                     | System.Collections.Generic.ICollection<DeviceGroupMemberChangeResult> | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: RemoveDeviceGroupMembers |
| `RemoveMembers`            | `RemoveMembersResult`            | System.Collections.Generic.IEnumerable<DeviceGroupMemberSelection> updates | Ack (success/failure)                                                 | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/device-groups/members/bulk/remove         |
| `RenameDeviceGroups`       | `RenameDeviceGroupsResult`       | RenameDeviceGroupsRequest request                                          | System.Collections.Generic.ICollection<DeviceGroupChangeResult>       | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: RenameDeviceGroups       |
| `RenameGroups`             | `RenameGroupsResult`             | System.Collections.Generic.IEnumerable<DeviceGroupName> renames            | Ack (success/failure)                                                 | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/device-groups/bulk/rename                 |

#### Queries

| Query             | Result                  | Shape (request)           | Result payload                                      | Source                     | Destination | Target (reply-to)          | Permissions                                           | Maps from                             |
| ----------------- | ----------------------- | ------------------------- | --------------------------------------------------- | -------------------------- | ----------- | -------------------------- | ----------------------------------------------------- | ------------------------------------- |
| `GetById`         | `GetByIdResult`         | System.Guid deviceGroupId | DeviceGroup                                         | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/device-groups/{deviceGroupId} |
| `GetDeviceGroups` | `GetDeviceGroupsResult` | —                         | System.Collections.Generic.ICollection<DeviceGroup> | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/device-groups                 |

### Reporting

#### Commands

| Command                            | Result                                   | Shape (request)                                                      | Result payload        | Source                     | Destination | Target (reply-to)          | Permissions                                           | Maps from                                                       |
| ---------------------------------- | ---------------------------------------- | -------------------------------------------------------------------- | --------------------- | -------------------------- | ----------- | -------------------------- | ----------------------------------------------------- | --------------------------------------------------------------- |
| `CreateReportRun`                  | `CreateReportRunResult`                  | DeviceReportRequest request                                          | System.Guid           | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/reports/device-report/runs                             |
| `CreateSubscriptionOfDeviceReport` | `CreateSubscriptionOfDeviceReportResult` | SubscriptionOfDeviceReportOptions create                             | System.Guid           | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/reports/device-report/subscriptions                    |
| `DeleteDeviceReportSubscriptions`  | `DeleteDeviceReportSubscriptionsResult`  | System.Guid subscriptionId                                           | System.Guid           | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/reports/device-report/subscriptions/{subscriptionId} |
| `DeleteReportRun`                  | `DeleteReportRunResult`                  | System.Guid reportRunId                                              | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/reports/runs/{reportRunId}                           |
| `ReportHeartbeat`                  | `ReportHeartbeatResult`                  | string source                                                        | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/health/heartbeat/{source}                               |
| `SendCompleteNotification`         | `SendCompleteNotificationResult`         | System.Guid reportRunId                                              | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/reports/runs/{reportRunId}/status                      |
| `TestFilePath`                     | `TestFilePathResult`                     | ReportFileTest model                                                 | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/reports/files/test-path                                |
| `UpdateSubscriptionOfDeviceReport` | `UpdateSubscriptionOfDeviceReportResult` | System.Guid subscriptionId, SubscriptionOfDeviceReportOptions update | System.Guid           | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/reports/device-report/subscriptions/{subscriptionId}    |

#### Queries

| Query                          | Result                               | Shape (request)                          | Result payload                                                   | Source                     | Destination                      | Target (reply-to)          | Permissions                                           | Maps from                                                     |
| ------------------------------ | ------------------------------------ | ---------------------------------------- | ---------------------------------------------------------------- | -------------------------- | -------------------------------- | -------------------------- | ----------------------------------------------------- | ------------------------------------------------------------- |
| `Download`                     | `DownloadResult`                     | System.Guid reportRunId                  | ReportData                                                       | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/reports/runs/{reportRunId}/data                       |
| `Download`                     | `DownloadResult`                     | System.Guid reportRunId, string name     | FileResponse                                                     | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET public/reports/download/excel/{reportRunId}               |
| `DownloadFile`                 | `DownloadFileResult`                 | System.Guid reportRunId, string filename | FileResponse                                                     | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/reports/runs/{reportRunId}/data/files/{filename}      |
| `GetCompletedRuns`             | `GetCompletedRunsResult`             | —                                        | System.Collections.Generic.ICollection<DeviceReportRuns>         | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/reports/device-report/subscriptions/runs/completed    |
| `GetDeviceReportSubscriptions` | `GetDeviceReportSubscriptionsResult` | —                                        | System.Collections.Generic.ICollection<DeviceReportSubscription> | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/reports/device-report/subscriptions                   |
| `GetHardwarePassword`          | `GetHardwarePasswordResult`          | System.Guid reportRunId                  | System.Collections.Generic.IDictionary<string, string>           | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/reports/device-report/runs/{reportRunId}/passwords    |
| `GetPerformanceReport`         | `GetPerformanceReportResult`         | —                                        | ServerPerformanceReport                                          | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/health/performance                                    |
| `GetPerformanceReports`        | `GetPerformanceReportsResult`        | GlobalPerformanceReportRequest request   | GlobalPerformanceReportResponse                                  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/global/performance                                   |
| `GetReportOverviews`           | `GetReportOverviewsResult`           | —                                        | GlobalReportOverviewResponse                                     | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/global/reports/overview                               |
| `GetReportRunStatus`           | `GetReportRunStatusResult`           | System.Guid reportRunId                  | ReportRunStatusReport                                            | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/reports/runs/{reportRunId}/status                     |
| `GetUpcomingRuns`              | `GetUpcomingRunsResult`              | —                                        | System.Collections.Generic.ICollection<UpcomingDeviceReport>     | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/reports/device-report/runs/upcoming                   |
| `ScheduledReportOverviewQuery` | `ScheduledReportOverviewQueryResult` | (see contract)                           | ScheduledReportOverviewQueryResult                               | toolbox-api (parent site)  | toolbox-api (managed/child site) | toolbox-api (parent site)  | Toolbox **ManagedSite** user (federated child site)   | SignalR /hubs/sites :: Reporting.ScheduledReportOverviewQuery |

#### Events

| Event                | Shape (payload)                                                             | Source               | Subscribers (target destinations) | Permissions                     | Purpose / maps from                    |
| -------------------- | --------------------------------------------------------------------------- | -------------------- | --------------------------------- | ------------------------------- | -------------------------------------- |
| `ReportRunCompleted` | Guid ReportRunId, Guid SubscriptionId, string FilePath, ReportStatus Status | toolbox-service-host | toolbox-api, notifications        | System scope (report processor) | A scheduled report finished generating |

### Notifications & Email

#### Commands

| Command                          | Result                                 | Shape (request)                  | Result payload            | Source                     | Destination | Target (reply-to)          | Permissions                                           | Maps from                                    |
| -------------------------------- | -------------------------------------- | -------------------------------- | ------------------------- | -------------------------- | ----------- | -------------------------- | ----------------------------------------------------- | -------------------------------------------- |
| `RemoveLocalConfigurationLegacy` | `RemoveLocalConfigurationLegacyResult` | —                                | Ack (success/failure)     | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/milestone/site/smtp-configuration |
| `SendEmail`                      | `SendEmailResult`                      | EmailModel model                 | Ack (success/failure)     | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/notifications/emails                |
| `SendTestEmail`                  | `SendTestEmailResult`                  | EmailTestConfiguration config    | Ack (success/failure)     | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/notifications/emails/test           |
| `SendVerification`               | `SendVerificationResult`               | EmailVerificationRequest request | EmailVerificationResponse | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/notifications/verify-email          |
| `UpdateLocalConfigurationLegacy` | `UpdateLocalConfigurationLegacyResult` | SmtpConfiguration update         | Ack (success/failure)     | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/milestone/site/smtp-configuration    |

#### Queries

| Query                         | Result                              | Shape (request) | Result payload       | Source                     | Destination                      | Target (reply-to)          | Permissions                                           | Maps from                                           |
| ----------------------------- | ----------------------------------- | --------------- | -------------------- | -------------------------- | -------------------------------- | -------------------------- | ----------------------------------------------------- | --------------------------------------------------- |
| `EmailTestQuery`              | `EmailTestQueryResult`              | (see contract)  | EmailTestQueryResult | toolbox-api (parent site)  | toolbox-api (managed/child site) | toolbox-api (parent site)  | Toolbox **ManagedSite** user (federated child site)   | SignalR /hubs/sites :: Notifications.EmailTestQuery |
| `GetLocalConfigurationLegacy` | `GetLocalConfigurationLegacyResult` | —               | SmtpConfiguration    | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/milestone/site/smtp-configuration           |

### Metadata (Tags, Priority, Maintenance)

#### Commands

| Command                  | Result                         | Shape (request)                                                 | Result payload        | Source                     | Destination | Target (reply-to)          | Permissions                                           | Maps from                                                   |
| ------------------------ | ------------------------------ | --------------------------------------------------------------- | --------------------- | -------------------------- | ----------- | -------------------------- | ----------------------------------------------------- | ----------------------------------------------------------- |
| `AssignTag`              | `AssignTagResult`              | HardwareTagBulkOperation operation                              | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/metadata/tags/hardware/bulk/assign                 |
| `AssignTag`              | `AssignTagResult`              | System.Guid hardwareId, string tagName                          | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/metadata/tags/hardware/{hardwareId}/{tagName}       |
| `AssignTag`              | `AssignTagResult`              | RecorderTagBulkOperation operation                              | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/metadata/tags/recorders/bulk/assign                |
| `AssignTag`              | `AssignTagResult`              | System.Guid recorderId, string tagName                          | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/metadata/tags/recorders/{recorderId}/{tagName}      |
| `CancelActiveSchedule`   | `CancelActiveScheduleResult`   | System.Guid scheduleId                                          | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/metadata/maintenance/schedules/{scheduleId}/cancel |
| `CreateSchedule`         | `CreateScheduleResult`         | ScheduleModel create                                            | System.Guid           | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/metadata/maintenance/schedules                     |
| `DeleteSchedule`         | `DeleteScheduleResult`         | System.Guid scheduleId                                          | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/metadata/maintenance/schedules/{scheduleId}      |
| `DisableMaintenanceMode` | `DisableMaintenanceModeResult` | System.Collections.Generic.IEnumerable<System.Guid> hardwareIds | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/metadata/maintenance/hardware/disable              |
| `DisableMaintenanceMode` | `DisableMaintenanceModeResult` | System.Collections.Generic.IEnumerable<System.Guid> recorderIds | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/metadata/maintenance/recorders/disable             |
| `EnableMaintenanceMode`  | `EnableMaintenanceModeResult`  | System.Collections.Generic.IEnumerable<System.Guid> hardwareIds | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/metadata/maintenance/hardware/enable               |
| `EnableMaintenanceMode`  | `EnableMaintenanceModeResult`  | System.Collections.Generic.IEnumerable<System.Guid> recorderIds | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/metadata/maintenance/recorders/enable              |
| `RemoveComment`          | `RemoveCommentResult`          | System.Guid hardwareId                                          | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/metadata/comments/hardware/{hardwareId}          |
| `RemoveTag`              | `RemoveTagResult`              | HardwareTagBulkOperation operation                              | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/metadata/tags/hardware/bulk/remove                 |
| `RemoveTag`              | `RemoveTagResult`              | System.Guid hardwareId, string tagName                          | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/metadata/tags/hardware/{hardwareId}/{tagName}    |
| `RemoveTag`              | `RemoveTagResult`              | RecorderTagBulkOperation operation                              | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/metadata/tags/recorders/bulk/remove                |
| `RemoveTag`              | `RemoveTagResult`              | System.Guid recorderId, string tagName                          | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/metadata/tags/recorders/{recorderId}/{tagName}   |
| `SetComment`             | `SetCommentResult`             | System.Guid hardwareId, HardwareCommentUpdate update            | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/metadata/comments/hardware/{hardwareId}             |
| `SetPriorityCritical`    | `SetPriorityCriticalResult`    | System.Collections.Generic.IEnumerable<System.Guid> hardwareIds | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/metadata/priority/hardware/critical                |
| `SetPriorityCritical`    | `SetPriorityCriticalResult`    | System.Collections.Generic.IEnumerable<System.Guid> recorderIds | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/metadata/priority/recorders/critical               |
| `SetPriorityNormal`      | `SetPriorityNormalResult`      | System.Collections.Generic.IEnumerable<System.Guid> hardwareIds | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/metadata/priority/hardware/normal                  |
| `SetPriorityNormal`      | `SetPriorityNormalResult`      | System.Collections.Generic.IEnumerable<System.Guid> recorderIds | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/metadata/priority/recorders/normal                 |
| `UpdateSchedule`         | `UpdateScheduleResult`         | System.Guid scheduleId, ScheduleModel create                    | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/metadata/maintenance/schedules/{scheduleId}         |

#### Queries

| Query                           | Result                                | Shape (request)        | Result payload                                                 | Source                     | Destination | Target (reply-to)          | Permissions                                           | Maps from                                       |
| ------------------------------- | ------------------------------------- | ---------------------- | -------------------------------------------------------------- | -------------------------- | ----------- | -------------------------- | ----------------------------------------------------- | ----------------------------------------------- |
| `GetAllTags`                    | `GetAllTagsResult`                    | —                      | System.Collections.Generic.ICollection<HardwareTagInformation> | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/tags/hardware/bulk             |
| `GetAllTags`                    | `GetAllTagsResult`                    | —                      | System.Collections.Generic.ICollection<RecorderTagInformation> | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/tags/recorders/bulk            |
| `GetCriticalHardware`           | `GetCriticalHardwareResult`           | —                      | System.Collections.Generic.ICollection<HardwarePriority>       | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/priority/hardware              |
| `GetCriticalRecorders`          | `GetCriticalRecordersResult`          | —                      | System.Collections.Generic.ICollection<RecorderPriority>       | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/priority/recorders             |
| `GetHardwareComment`            | `GetHardwareCommentResult`            | System.Guid hardwareId | HardwareComment                                                | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/comments/hardware/{hardwareId} |
| `GetHardwareComments`           | `GetHardwareCommentsResult`           | —                      | System.Collections.Generic.ICollection<HardwareComment>        | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/comments/hardware              |
| `GetHardwareInMaintenanceMode`  | `GetHardwareInMaintenanceModeResult`  | —                      | System.Collections.Generic.ICollection<MaintenanceHardware>    | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/maintenance/hardware           |
| `GetHardwareWithTag`            | `GetHardwareWithTagResult`            | string tagName         | System.Collections.Generic.ICollection<System.Guid>            | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/tags/hardware/{tagName}        |
| `GetRecordersInMaintenanceMode` | `GetRecordersInMaintenanceModeResult` | —                      | System.Collections.Generic.ICollection<MaintenanceRecorder>    | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/maintenance/recorders          |
| `GetRecordersWithTag`           | `GetRecordersWithTagResult`           | string tagName         | System.Collections.Generic.ICollection<System.Guid>            | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/tags/recorders/{tagName}       |
| `GetSchedules`                  | `GetSchedulesResult`                  | —                      | System.Collections.Generic.ICollection<ScheduleResource>       | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/maintenance/schedules          |
| `GetTagNames`                   | `GetTagNamesResult`                   | —                      | System.Collections.Generic.ICollection<string>                 | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/tags/hardware                  |
| `GetTagNames`                   | `GetTagNamesResult`                   | —                      | System.Collections.Generic.ICollection<string>                 | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/tags/recorders                 |
| `GetTagsForHardware`            | `GetTagsForHardwareResult`            | System.Guid hardwareId | System.Collections.Generic.ICollection<string>                 | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/tags/hardware/{hardwareId}     |
| `GetTagsForRecorder`            | `GetTagsForRecorderResult`            | System.Guid recorderId | System.Collections.Generic.ICollection<string>                 | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/tags/recorders/{recorderId}    |
| `GetUpcomingMaintenance`        | `GetUpcomingMaintenanceResult`        | —                      | System.Collections.Generic.ICollection<UpcomingMaintenance>    | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/metadata/maintenance/schedules/upcoming |

#### Events

| Event                  | Shape (payload)                                         | Source      | Subscribers (target destinations) | Permissions                                      | Purpose / maps from                                  |
| ---------------------- | ------------------------------------------------------- | ----------- | --------------------------------- | ------------------------------------------------ | ---------------------------------------------------- |
| `MaintenanceCancelled` | Guid ScheduleId                                         | toolbox-api | live-monitor-service              | Toolbox user action → installation-local pub/sub | A scheduled maintenance window was cancelled         |
| `MaintenanceScheduled` | Guid TargetId, DateTimeOffset Start, DateTimeOffset End | toolbox-api | live-monitor-service              | Toolbox user action → installation-local pub/sub | Maintenance window was scheduled (suppresses alerts) |

### Users, Auth & Audit

#### Commands

| Command                        | Result                               | Shape (request)                                                | Result payload             | Source                     | Destination     | Target (reply-to)          | Permissions                                           | Maps from                                           |
| ------------------------------ | ------------------------------------ | -------------------------------------------------------------- | -------------------------- | -------------------------- | --------------- | -------------------------- | ----------------------------------------------------- | --------------------------------------------------- |
| `AcceptEula`                   | `AcceptEulaResult`                   | —                                                              | Ack (success/failure)      | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/integrations/eula                          |
| `Authenticate`                 | `AuthenticateResult`                 | AuthenticationRequest request                                  | AuthenticationResponse     | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: Authenticate              |
| `Authenticate`                 | `AuthenticateResult`                 | AuthenticationModel model                                      | AuthenticationTokens       | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/authentication/authenticate                |
| `Authenticate`                 | `AuthenticateResult`                 | WebAuthenticationModel model                                   | WebAuthenticationDetails   | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/authentication/web/authenticate            |
| `AuthenticateAsWindowsUser`    | `AuthenticateAsWindowsUserResult`    | —                                                              | AuthenticationResponse     | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: AuthenticateAsWindowsUser |
| `AuthenticateBasicUser`        | `AuthenticateBasicUserResult`        | SystemBasicAuthenticationModel model                           | SystemAuthenticationTokens | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/system/authenticate/basic                  |
| `AuthenticateWindowsUser`      | `AuthenticateWindowsUserResult`      | —                                                              | AuthenticationTokens       | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/authentication/authenticate/windows        |
| `AuthenticateWindowsUser`      | `AuthenticateWindowsUserResult`      | SystemWindowsAuthenticationModel model                         | SystemAuthenticationTokens | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/system/authenticate/windows                |
| `CreateSessionToken`           | `CreateSessionTokenResult`           | —                                                              | WebTokenModel              | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/authentication/web/token                   |
| `Refresh`                      | `RefreshResult`                      | RefreshModel model                                             | AuthenticationTokens       | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/authentication/refresh                     |
| `RefreshWebToken`              | `RefreshWebTokenResult`              | WebTokenModel model                                            | WebAuthenticationDetails   | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/authentication/web/refresh                 |
| `RevokeRefreshToken`           | `RevokeRefreshTokenResult`           | —                                                              | Ack (success/failure)      | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/authentication/web/revoke                |
| `TestAuthentication`           | `TestAuthenticationResult`           | AuthenticationRequest request                                  | AuthenticationTestResponse | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: TestAuthentication        |
| `TestConnection`               | `TestConnectionResult`               | TestModel request                                              | Ack (success/failure)      | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/authentication/test                        |
| `TestConnectionForServiceUser` | `TestConnectionForServiceUserResult` | —                                                              | Ack (success/failure)      | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/authentication/test/service-user           |
| `TestWindowsAuthentication`    | `TestWindowsAuthenticationResult`    | —                                                              | AuthenticationTestResponse | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: TestWindowsAuthentication |
| `UpdateSystemPreferences`      | `UpdateSystemPreferencesResult`      | System.Collections.Generic.IDictionary<string, string> updates | Ack (success/failure)      | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PATCH api/preferences/system                        |
| `UpdateUserPreferences`        | `UpdateUserPreferencesResult`        | System.Collections.Generic.IDictionary<string, string> updates | Ack (success/failure)      | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PATCH api/preferences/user                          |

#### Queries

| Query                     | Result                          | Shape (request)                   | Result payload                                              | Source                     | Destination                      | Target (reply-to)          | Permissions                                                                      | Maps from                                          |
| ------------------------- | ------------------------------- | --------------------------------- | ----------------------------------------------------------- | -------------------------- | -------------------------------- | -------------------------- | -------------------------------------------------------------------------------- | -------------------------------------------------- |
| `AuditLogEntryQuery`      | `AuditLogEntryQueryResult`      | (see contract)                    | AuditLogEntryQueryResult                                    | toolbox-api (parent site)  | toolbox-api (managed/child site) | toolbox-api (parent site)  | Toolbox **ManagedSite** user (federated child site)                              | SignalR /hubs/sites :: Auditing.AuditLogEntryQuery |
| `AuthenticatedPing`       | `AuthenticatedPingResult`       | —                                 | Ack (success/failure)                                       | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | GET api/health/authenticated-ping                  |
| `GetAuditLogs`            | `GetAuditLogsResult`            | string category, string eventType | GlobalAuditLogResponse                                      | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | GET api/global/auditing/logs                       |
| `GetCurrentToken`         | `GetCurrentTokenResult`         | —                                 | string                                                      | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | GET api/milestone/token                            |
| `GetDiscoveryInformation` | `GetDiscoveryInformationResult` | —                                 | DiscoveryModel                                              | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | GET api/discovery                                  |
| `GetLogs`                 | `GetLogsResult`                 | string category, string eventType | System.Collections.Generic.ICollection<AuditLogEntryModel>  | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | GET api/auditing/logs                              |
| `GetSystemPreferences`    | `GetSystemPreferencesResult`    | —                                 | System.Collections.Generic.IDictionary<string, string>      | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | GET api/preferences/system                         |
| `GetToken`                | `GetTokenResult`                | —                                 | MilestoneToken                                              | toolbox-api                | milestone-proxy                  | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token                             | milestone-proxy client :: GetToken                 |
| `GetUserAccess`           | `GetUserAccessResult`           | string userId                     | UserAccessDto                                               | any authorised caller      | core-api                         | any authorised caller      | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET (controller-root)                              |
| `GetUserAccess`           | `GetUserAccessResult`           | —                                 | UserAccessResponse                                          | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | GET api/me/access                                  |
| `GetUserAcknowledgements` | `GetUserAcknowledgementsResult` | —                                 | System.Collections.Generic.ICollection<UserAcknowledgement> | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | GET api/me/acknowledgements                        |
| `GetUserPreferences`      | `GetUserPreferencesResult`      | —                                 | System.Collections.Generic.IDictionary<string, string>      | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | GET api/preferences/user                           |
| `UseRolePermissions`      | `UseRolePermissionsResult`      | —                                 | bool                                                        | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | GET api/milestone/user-role                        |

### Bot Integrations

#### Commands

| Command               | Result                      | Shape (request)                      | Result payload        | Source                     | Destination   | Target (reply-to)          | Permissions                                           | Maps from                                               |
| --------------------- | --------------------------- | ------------------------------------ | --------------------- | -------------------------- | ------------- | -------------------------- | ----------------------------------------------------- | ------------------------------------------------------- |
| `Command`             | `CommandResult`             | CommandContext commandContext        | Ack (success/failure) | any authorised caller      | slack-bot-web | any authorised caller      | TBD                                                   | POST (controller-root)                                  |
| `Disconnect`          | `DisconnectResult`          | System.Guid connectionId             | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api   | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/integrations/bots/connections/{connectionId} |
| `GenerateToken`       | `GenerateTokenResult`       | GenerateTokenRequest request         | BotTokenResponse      | toolbox-api (UI / clients) | toolbox-api   | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/integrations/bots/token                        |
| `HandleEvent`         | `HandleEventResult`         | JsonDocument body                    | Ack (success/failure) | any authorised caller      | slack-bot-web | any authorised caller      | TBD                                                   | POST (controller-root)                                  |
| `InteractiveEndpoint` | `InteractiveEndpointResult` | string payload                       | Ack (success/failure) | any authorised caller      | slack-bot-web | any authorised caller      | TBD                                                   | POST (controller-root)                                  |
| `PostAsync`           | `PostAsyncResult`           | —                                    | Task                  | any authorised caller      | slack-bot-web | any authorised caller      | TBD                                                   | POST (controller-root)                                  |
| `PostIFormCollection` | `PostIFormCollectionResult` | IFormCollection form, IFormFile file | Ack (success/failure) | any authorised caller      | slack-bot-web | any authorised caller      | TBD                                                   | POST (controller-root)                                  |
| `RevokeToken`         | `RevokeTokenResult`         | —                                    | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api   | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/integrations/bots/token                      |

#### Queries

| Query                    | Result                         | Shape (request) | Result payload                                             | Source                     | Destination                      | Target (reply-to)          | Permissions                                           | Maps from                                            |
| ------------------------ | ------------------------------ | --------------- | ---------------------------------------------------------- | -------------------------- | -------------------------------- | -------------------------- | ----------------------------------------------------- | ---------------------------------------------------- |
| `GetConnections`         | `GetConnectionsResult`         | —               | System.Collections.Generic.ICollection<BotConnectionModel> | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/integrations/bots/connections                |
| `GetExistingToken`       | `GetExistingTokenResult`       | —               | BotTokenResponse                                           | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/integrations/bots/token                      |
| `GetServerInformation`   | `GetServerInformationResult`   | —               | MilestoneServerInformation                                 | toolbox-api                | milestone-proxy                  | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: GetServerInformation       |
| `GetStatus`              | `GetStatusResult`              | —               | BotIntegrationStatusResponse                               | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/integrations/bots/status                     |
| `ServerPerformanceQuery` | `ServerPerformanceQueryResult` | (see contract)  | ServerPerformanceQueryResult                               | toolbox-api (parent site)  | toolbox-api (managed/child site) | toolbox-api (parent site)  | Toolbox **ManagedSite** user (federated child site)   | SignalR /hubs/sites :: Health.ServerPerformanceQuery |

#### Events

| Event                      | Shape (payload)                         | Source                             | Subscribers (target destinations)         | Permissions                               | Purpose / maps from                                  |
| -------------------------- | --------------------------------------- | ---------------------------------- | ----------------------------------------- | ----------------------------------------- | ---------------------------------------------------- |
| `BotServerConnected`       | Guid InstallationId, string WorkspaceId | slack-bot-web                      | toolbox-api                               | Bot connectivity token                    | A Toolbox server connected to a Slack workspace      |
| `BotServerDisconnected`    | Guid InstallationId, string WorkspaceId | slack-bot-web                      | toolbox-api                               | Bot connectivity token                    | A Toolbox server disconnected from a Slack workspace |
| `ConnectivityTokenRevoked` | string TokenId, Guid InstallationId     | toolbox-api                        | slack-bot-web                             | Toolbox user action                       | A bot connectivity token was revoked                 |
| `SlackEvent`               | (see contract)                          | toolbox-api / live-monitor-service | slack-bot-web → posts Slack notifications | Bot connectivity token (workspace-scoped) | bot contract `SlackEvent`                            |

### HubSpot / License Sync

#### Events

| Event                    | Shape (payload)                           | Source       | Subscribers (target destinations) | Permissions                      | Purpose / maps from                         |
| ------------------------ | ----------------------------------------- | ------------ | --------------------------------- | -------------------------------- | ------------------------------------------- |
| `LicenseSyncedToHubSpot` | Guid InstallationId, string HubSpotDealId | hubspot-sync | (none — terminal)                 | Internal/system (scheduled sync) | License/installation data pushed to HubSpot |

### Platform & Health

#### Commands

| Command | Result        | Shape (request)             | Result payload        | Source                     | Destination | Target (reply-to)          | Permissions                                           | Maps from                      |
| ------- | ------------- | --------------------------- | --------------------- | -------------------------- | ----------- | -------------------------- | ----------------------------------------------------- | ------------------------------ |
| `Start` | `StartResult` | ServiceStateRequest request | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/health/services/start |
| `Stop`  | `StopResult`  | ServiceStateRequest request | Ack (success/failure) | toolbox-api (UI / clients) | toolbox-api | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/health/services/stop  |

#### Queries

| Query                     | Result                          | Shape (request)      | Result payload                | Source                     | Destination                      | Target (reply-to)          | Permissions                                                                      | Maps from                                                        |
| ------------------------- | ------------------------------- | -------------------- | ----------------------------- | -------------------------- | -------------------------------- | -------------------------- | -------------------------------------------------------------------------------- | ---------------------------------------------------------------- |
| `GetHeartbeatDetails`     | `GetHeartbeatDetailsResult`     | string source        | HeartbeatDetails              | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | GET api/health/heartbeat/{source}                                |
| `GetNetworkInformation`   | `GetNetworkInformationResult`   | —                    | NetworkInformation            | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | GET api/system/network                                           |
| `GetStatus`               | `GetStatusResult`               | ServiceType? service | ServiceStatus                 | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | GET api/health/services/status                                   |
| `GetStatus`               | `GetStatusResult`               | System.Guid taskId   | TaskStatus                    | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local                            | GET api/tasks/status/{taskId}                                    |
| `NetworkInformationQuery` | `NetworkInformationQueryResult` | (see contract)       | NetworkInformationQueryResult | toolbox-api (parent site)  | toolbox-api (managed/child site) | toolbox-api (parent site)  | Toolbox **ManagedSite** user (federated child site)                              | SignalR /hubs/sites :: SystemInformation.NetworkInformationQuery |
| `Ping`                    | `PingResult`                    | —                    | Ack (success/failure)         | any authorised caller      | core-api                         | any authorised caller      | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET (controller-root)                                            |
| `Ping`                    | `PingResult`                    | —                    | Ack (success/failure)         | monitoring/infra           | core-api                         | monitoring/infra           | Toolbox installation token — `cloud.api.*`, active `InstallationId`, Toolbox app | GET /api/ping                                                    |
| `Ping`                    | `PingResult`                    | —                    | bool                          | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Anonymous (liveness/readiness)                                                   | GET api/health/ping                                              |

### Other

#### Commands

| Command                        | Result                               | Shape (request)                                     | Result payload                                               | Source                     | Destination     | Target (reply-to)          | Permissions                                           | Maps from                                     |
| ------------------------------ | ------------------------------------ | --------------------------------------------------- | ------------------------------------------------------------ | -------------------------- | --------------- | -------------------------- | ----------------------------------------------------- | --------------------------------------------- |
| `ActivateCloudActivation`      | `ActivateCloudActivationResult`      | CloudActivationConfiguration configuration          | FileResponse                                                 | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/cloud/activate                       |
| `AddMilestoneDataSource`       | `AddMilestoneDataSourceResult`       | MilestoneDataSourceModel site                       | System.Guid                                                  | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/data-sources/milestone               |
| `DeleteDataSourcesMilestone`   | `DeleteDataSourcesMilestoneResult`   | System.Guid sourceId                                | System.Guid                                                  | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | DELETE api/data-sources/milestone/{sourceId}  |
| `DisableHttps`                 | `DisableHttpsResult`                 | HardwareSelection selection                         | System.Collections.Generic.ICollection<HardwareChangeResult> | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: DisableHttps        |
| `EnableHttps`                  | `EnableHttpsResult`                  | HardwareSelection selection                         | System.Collections.Generic.ICollection<HardwareChangeResult> | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: EnableHttps         |
| `GenerateIncidentNumber`       | `GenerateIncidentNumberResult`       | —                                                   | int                                                          | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/incidents/generate                   |
| `MeAcknowledgements`           | `MeAcknowledgementsResult`           | string type                                         | FileResponse                                                 | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/me/acknowledgements/{type}            |
| `ResolveLatestIncident`        | `ResolveLatestIncidentResult`        | System.Guid entityId                                | int                                                          | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/incidents/offline/resolve/{entityId} |
| `SaveLocalConfigurationLegacy` | `SaveLocalConfigurationLegacyResult` | LegacySiteConfiguration update                      | Ack (success/failure)                                        | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/milestone/site/configuration          |
| `SystemRefresh`                | `SystemRefreshResult`                | SystemRefreshModel model                            | SystemAuthenticationTokens                                   | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/system/refresh                       |
| `TestCloudConnection`          | `TestCloudConnectionResult`          | —                                                   | CloudConnectionTestResponse                                  | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/cloud/connection/test                |
| `TrackIncident`                | `TrackIncidentResult`                | TrackOfflineIncidentRequest model                   | int                                                          | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | POST api/incidents/offline/track              |
| `UpdateAddresses`              | `UpdateAddressesResult`              | CloudAddressConfigurationUpdateRequest model        | FileResponse                                                 | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/cloud/configuration/addresses         |
| `UpdateMilestoneDataSource`    | `UpdateMilestoneDataSourceResult`    | System.Guid sourceId, MilestoneDataSourceModel site | System.Guid                                                  | toolbox-api (UI / clients) | toolbox-api     | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | PUT api/data-sources/milestone/{sourceId}     |
| `UpdateSettings`               | `UpdateSettingsResult`               | CameraStreamSettingsRequest request                 | CameraStreamSettingsResponse                                 | toolbox-api                | milestone-proxy | toolbox-api                | Toolbox user — proxied Milestone (VMS) session token  | milestone-proxy client :: UpdateSettings      |

#### Queries

| Query                      | Result                           | Shape (request) | Result payload                                                      | Source                     | Destination                      | Target (reply-to)          | Permissions                                           | Maps from                                        |
| -------------------------- | -------------------------------- | --------------- | ------------------------------------------------------------------- | -------------------------- | -------------------------------- | -------------------------- | ----------------------------------------------------- | ------------------------------------------------ |
| `CloudConnectionTestQuery` | `CloudConnectionTestQueryResult` | (see contract)  | CloudConnectionTestQueryResult                                      | toolbox-api (parent site)  | toolbox-api (managed/child site) | toolbox-api (parent site)  | Toolbox **ManagedSite** user (federated child site)   | SignalR /hubs/sites :: Cloud.ConnectionTestQuery |
| `GetAddresses`             | `GetAddressesResult`             | —               | CloudAddressConfigurationResponse                                   | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/cloud/configuration/addresses            |
| `GetConfigurationLegacy`   | `GetConfigurationLegacyResult`   | —               | LegacySiteConfiguration                                             | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/milestone/site/configuration             |
| `GetDataSourcesMilestone`  | `GetDataSourcesMilestoneResult`  | —               | System.Collections.Generic.ICollection<MilestoneDataSourceResource> | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/data-sources/milestone                   |
| `GetLocalSiteLegacy`       | `GetLocalSiteLegacyResult`       | —               | LocalSiteInformation                                                | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/milestone/site                           |
| `GetSessionInfo`           | `GetSessionInfoResult`           | —               | MilestoneSessionInfo                                                | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/milestone/session                        |
| `GetTimeZones`             | `GetTimeZonesResult`             | —               | System.Collections.Generic.ICollection<TimeZone>                    | toolbox-api (UI / clients) | toolbox-api                      | toolbox-api (UI / clients) | Toolbox user (`User` or `System`), installation-local | GET api/timezones                                |

### Bot Integrations — RPC commands (Slack bot ⇄ Toolbox)

These already use a command/response RPC over SignalR and map almost 1:1 onto bus commands.

| Command                            | Request                                         | Result                 | Source        | Destination          | Permissions                               |
| ---------------------------------- | ----------------------------------------------- | ---------------------- | ------------- | -------------------- | ----------------------------------------- |
| `CameraSearch`                     | `CameraSearchRequest`                           | `CameraSearchResponse` | slack-bot-web | toolbox-api (server) | Bot connectivity token (workspace-scoped) |
| `CameraStatus`                     | `CameraStatusRequest`                           | `CameraStatusResponse` | slack-bot-web | toolbox-api (server) | Bot connectivity token (workspace-scoped) |
| `ConnectServerToWorkspace`         | `ConnectServerDto`                              | `GenericDto`           | slack-bot-web | toolbox-api (server) | Bot connectivity token (workspace-scoped) |
| `DisconnectFromWorkspaceOnBotSide` | `DisconnectServerFromWorkspaceOnBotSideRequest` | `GenericDto`           | slack-bot-web | toolbox-api (server) | Bot connectivity token (workspace-scoped) |
| `DisconnectServerFromWorkspace`    | `DisconnectServerCommandDto`                    | `GenericDto`           | slack-bot-web | toolbox-api (server) | Bot connectivity token (workspace-scoped) |
| `RegisterConnectivityToken`        | `RegisterConnectivityTokenRequest`              | `GenericDto`           | slack-bot-web | toolbox-api (server) | Bot connectivity token (workspace-scoped) |
| `RevokeConnectivityToken`          | `RevokeConnectivityTokenRequest`                | `GenericDto`           | slack-bot-web | toolbox-api (server) | Bot connectivity token (workspace-scoped) |
| `SystemStatus`                     | `SystemStatusRequest`                           | `SystemStatusResponse` | slack-bot-web | toolbox-api (server) | Bot connectivity token (workspace-scoped) |

### Asynchronous / background work (today's in-memory queues → bus commands & events)

These in-memory queues become first-class bus messages once work crosses a process boundary.

| Suggested message                | Kind    | Owning service       | Payload (today)                                                                     | Maps from                                                         |
| -------------------------------- | ------- | -------------------- | ----------------------------------------------------------------------------------- | ----------------------------------------------------------------- |
| `(internal background work)`     | Command | ai-analysis-api      | —                                                                                   | `BackgroundWorkItem` (CameraSnapshotAnalysisController)           |
| `CheckDeviceState`               | Command | hardware-library     | —                                                                                   | `StateCheckQueueItem` (StateCheckBackgroundService)               |
| `(Slack slash-command dispatch)` | Command | slack-bot-web        | —                                                                                   | `CommandContext` (SlackServiceCollectionExtensions)               |
| `(Slack event dispatch)`         | Event   | slack-bot-web        | —                                                                                   | `EventEnvelope` (EventProcessingBackgroundService)                |
| `(Slack interaction dispatch)`   | Command | slack-bot-web        | —                                                                                   | `SlackInteraction` (SlackInteractionBackgroundService)            |
| `(internal background work)`     | Command | toolbox-api          | —                                                                                   | `BackgroundTaskInfo` (where)                                      |
| `CheckDeviceState`               | Command | toolbox-api          | —                                                                                   | `StateCheckQueueItem` (FirmwareUpdateQueueItemHandler)            |
| `UpdateFirmware`                 | Command | toolbox-api          | —                                                                                   | `UpdateQueueItem` (FirmwareServiceCollectionExtensions)           |
| `AnalyzeCameraSnapshot`          | Command | toolbox-service-host | —                                                                                   | `ImageAnalysisQueue.Message` (CloudAIServiceCollectionExtensions) |
| `AnalyzeCameraSnapshot`          | Command | toolbox-service-host | CameraKeys Camera, ImageAnalysisCameraScope Scope, ImageAnalysisTimeOfDay TimeOfDay | `Message` (record)                                                |
| `CheckDeviceState`               | Command | toolbox-service-host | —                                                                                   | `StateCheckQueueItem` (DeviceInformationJob)                      |

## Coverage note

- Commands/queries are derived from **every** REST/SOAP/SignalR operation in the inventory (overloads de-duplicated): 190 commands, 170 queries.
- Events combine the existing `IServiceEvent`, bot state events, live-monitoring state changes, and a curated set of cross-service domain facts that today are implicit in HTTP side effects.
- `Result` payloads are the unwrapped response types (stripped of `Task`/`ActionResult`/`ApiResponse`).
- Names follow the conventions above; where the source method was already imperative (e.g. `RenameRecorders`) it is kept; queries keep their `Get*`/`List*` verb.
- Permissions are derived defaults from the owning endpoint policy — confirm per message.

