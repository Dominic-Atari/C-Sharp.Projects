# N.LMS

Modular .NET 8 Learning Management System: `WebApi` ↔ `Manager` (intercepted) ↔ `Engine` / `Accessor` ↔ `EF Core`, with `Utility` services at the bottom and a Castle-DynamicProxy interceptor pipeline (the **Ifx** layer) wrapping every Manager.

## Solution layout

```
N.LMS.Backend/
├── Database/                                                ← EF entities, factory, DbUp migrations
├── Common/
│   ├── N.LMS.Common.Interface/                              ← contracts, markers (IComponent, IUtility, IEngine, IProxyEnabledSubsystem)
│   └── N.LMS.Common.Service/                                ← DI integration (Registrar w/ Castle proxies, ServiceProxyGenerator)
├── Ifx/
│   └── N.LMS.Ifx.Interceptor/                               ← AsyncMethodInterceptorBase + 3 real interceptors
├── Services/
│   ├── Utility/                                             ← bottom of the graph (IUtility, ServiceBase)
│   │   ├── Context/   (Shape C: multi-verb + state, Scoped)
│   │   ├── DateTime/  (Shape A: property-only, Singleton)
│   │   └── Logging/   (Shape B: single verb + polymorphic Request, Singleton)
│   ├── Engine/                                              ← IEngine, no interceptors
│   │   └── Validation/
│   ├── Accessor/                                            ← IComponent, no interceptors, owns DB
│   │   └── User / Course / Enrollment / Module / Lesson / System
│   └── Manager/                                             ← IProxyEnabledSubsystem, full interceptor pipeline
│       └── Admin / Learning
└── Clients/
    └── N.LMS.Client.WebApi/                                 ← ASP.NET Core 8 entry point
```

## The four layers (marker → wrap)

| | Accessor | Engine | Manager | Utility |
|---|---|---|---|---|
| Marker | `IComponent` | `IEngine : IComponent` | `IProxyEnabledSubsystem : IComponent` | `IUtility : IComponent` |
| Base class | `ProxyEnabledServiceBase` | `ProxyEnabledServiceBase` | `ProxyEnabledServiceBase` | `ServiceBase` (no proxy) |
| Touches DB | yes (`DatabaseFactory`) | sometimes (reads) | no | rarely (`Context` is the exception) |
| Interceptors | none | none | **Exception → Context → Validation** | none |
| Lifetime | `Scoped` | `Scoped` | `Scoped` | `Singleton` (or `Scoped` for state, e.g. `Context`) |
| Called by | Managers | Managers, interceptors | Clients | Everyone |

## The Ifx pipeline

When a manager registers with interceptors:

```csharp
TypeRegistration.New<IAdminManager, AdminManager>(
    interceptors:
    [
        new ExceptionHandlingInterceptor(),
        new ContextBuildingInterceptor(),
        new ValidationEngineInterceptor(new Mapper())
    ]
);
```

`ServiceRegistrar` recognises the non-empty interceptor list and:

1. Registers the concrete class under itself so DI can construct it normally.
2. Registers the interface as a factory that builds the concrete instance and wraps it in a Castle dynamic proxy with the three interceptors.

Every call to `IAdminManager.Store(...)` from `ServiceProxyGenerator.ProxyForService<IAdminManager>()` therefore goes through:

```
ExceptionHandlingInterceptor   ← outermost: catches, logs (ILoggingUtility), converts to typed Error
  ContextBuildingInterceptor   ← reads WebContext/AnonymousContext, derives UserContext
    ValidationEngineInterceptor← Mapper(Manager-request → Engine-validate-request), short-circuits on failure
      AdminManager.Handle(...) ← the actual handler
```

`AsyncMethodInterceptorBase` (over `Castle.Core.AsyncInterceptor`'s `IAsyncInterceptor`) gives every interceptor the same shape: `InterceptMethodInvocation(IInvocationProceedInfo, IInvocation)` + a reflection trick that types the work to `Task<TResult>`.

`RequestBase.CreateResultFromRequest<TResult>()` is the **naming convention engine**: it string-substitutes `Request → Result` in the AQN to fabricate the right concrete failure envelope. Break the naming, get a `TypeLoadException` immediately.

## End-to-end flow

```
HTTP POST /Catalog/Enroll
        │ [Authorize] → JWT validated, HttpContext.User populated
        ▼
  CatalogController.Enroll
        │ HttpContext.ToWebContext()              (raw claims)
        │ using var proxy = new ServiceProxyGenerator(webContext)
        │   ├── creates a DI scope
        │   ├── deposits WebContext into IContextUtility (raw — NOT yet derived)
        │   └── ServiceLocator.EnterScope(...)
        │ proxy.ProxyForService<ILearningManager>()           ← Castle proxy
        ▼
  ExceptionHandlingInterceptor  (try/catch wrapper around everything below)
    ContextBuildingInterceptor  (IContextUtility.BuildAmbientContext → derives UserContext)
      ValidationEngineInterceptor (Mapper → Engine.Validate; short-circuit on Errors)
        LearningManager.Handle(EnrollInCourseRequest)
          ├── ProxyForService<IContextUtility>().GetRequiredContext<UserContext>()
          └── ProxyForService<IEnrollmentAccessor>().Store(...)
                └── DatabaseFactory.CreateContext() → MySQL

HTTP 200 ◄── Mapper.Map<EnrollInCourseResponse>(result) ◄── ApiControllerBase.CreateActionResult(response)
```

## Three Utility shapes (pick whichever fits)

| Shape | Interface surface | Example |
|---|---|---|
| **A — property-only** | just properties / methods | `IDateTimeUtility { UtcNow, UtcDateNow, UtcNowOffset }` |
| **B — single verb + polymorphic Request** | `void Log(LogRequestBase r)` dispatches on `r`'s concrete type | `ILoggingUtility` (EventLogRequest / ExceptionLogRequest / TraceLogRequest) |
| **C — multi-verb + state** | full set of methods, internal mutable state | `IContextUtility` (`GetContext`, `SetTypedContext`, `GetRequiredContext<T>`, `BuildAmbientContext`) |

Utilities use `ServiceBase`, not `ProxyEnabledServiceBase` — they're leaf nodes and don't need `ProxyForService<>()`. Lifetime is tuned per resource:

```csharp
TypeRegistration.New<IDateTimeUtility, DateTimeUtility>(LifetimeScope.Singleton)   // pure
TypeRegistration.New<ILoggingUtility,  LoggingUtility>(LifetimeScope.Singleton)    // stateless wrapper
TypeRegistration.New<IContextUtility,  ContextUtility>(LifetimeScope.Scoped)       // per-call-graph state
```

## How everything is registered

`Clients/N.LMS.Client.WebApi/Hosting.cs` concatenates every layer's `Hosting.Registrations` into one array:

```csharp
public static readonly RegistrationBase[] Registrations =
[
    ..N.LMS.Common.Interface.Hosting.Registrations,                  // (empty stub)
    ..N.LMS.Utility.Context.Service.Hosting.Registrations,           // Scoped
    ..N.LMS.Utility.DateTime.Service.Hosting.Registrations,          // Singleton
    ..N.LMS.Utility.Logging.Service.Hosting.Registrations,           // Singleton
    ..N.LMS.Engine.Validation.Service.Hosting.Registrations,
    ..N.LMS.Manager.Admin.Service.Hosting.Registrations,             // intercepted
    ..N.LMS.Manager.Learning.Service.Hosting.Registrations,          // intercepted
    ..N.LMS.Accessor.User.Service.Hosting.Registrations,
    // … the other accessors …
];
```

`Program.cs` then:

```csharp
var registrationBuilder = new RegistrationBuilder(Hosting.Registrations);
ServiceRegistrar.RegisterServices(builder.Services, registrationBuilder);
var app = builder.Build();
ServiceProxyHost.Configure(app.Services);                            // root provider for SPG
```

## Adding things

**A new accessor** — copy `Services/Accessor/Lesson/` → rename. `Load`/`Store`/`Delete` partial dispatchers + one handler file per request. `Hosting.cs` registers with no interceptors. Optional DbUp script.

**A new engine** — copy `Services/Engine/Validation/` → rename. `IEngine` marker. Pattern-match on `request.GetType()` / `request.InboundRequest?.GetType()` inside the engine. `Hosting.cs` registers with no interceptors.

**A new manager** — copy `Services/Manager/Admin/` → rename. `IProxyEnabledSubsystem`. Add a `Mapper.cs` for Manager-request → Engine-validate-request. `Hosting.cs` registers with `[ExceptionHandlingInterceptor, ContextBuildingInterceptor, ValidationEngineInterceptor(new Mapper())]`.

**A new utility** — `Services/Utility/<Name>/Interface/` + `Service/`. `IUtility` marker. Inherit `ServiceBase`. Pick a shape (A / B / C). Choose a lifetime in `Hosting.cs`.

**A new interceptor** — new class under `Ifx/N.LMS.Ifx.Interceptor/` inheriting `AsyncMethodInterceptorBase`. Guard `invocation.InvocationTarget is IProxyEnabledSubsystem`. Add to the `interceptors:` array of any manager that needs it. Order matters — outer wraps inner; exception handling stays outermost.

**A new endpoint** — `Request/<Area>/XxxRequest.cs` + `Response/<Area>/XxxResponse.cs` records. `CreateMap<XxxResult, XxxResponse>()` in `Clients/.../Mapper.cs`. New action on a controller: build context → `ProxyForService<IXxxManager>()` → build manager request inline → call → `Mapper.Map<XxxResponse>` → `CreateActionResult`.

## Build, test, run

```bash
dotnet build N.LMS.Backend.sln                              # 38 projects, ~317 source files, 0 warnings, 0 errors
dotnet test  N.LMS.Backend.sln                              # 25/25 tests pass
dotnet run   --project Clients/N.LMS.Client.WebApi  # http://localhost:5180 + Swagger in Development
```

## Migrations

```bash
N_LMS_NILE_DB_CONNECTION_STRING="Server=…;Database=NileDB;User=…;Password=…;" \
    dotnet run --project Database/N.LMS.Database.DbUp
```
