## Purpose

Short, actionable notes to help an AI coding agent be productive in this repository. Focused on the Roslyn source generator that creates cache-decorator wrappers and the small demo that shows how to use them.

## Big-picture architecture (what to know quickly)

- This repo contains a Roslyn incremental source generator that finds methods annotated with `[CacheDecorated]` and generates two artifacts per containing type:
  - `{DecoratedName}CacheDecorator.g.cs` — a partial class that wraps the original type and applies `IMemoryCache` logic.
  - `{DecoratedName}ServiceCollectionExtension.g.cs` — an `IServiceCollection` extension to register the implementation and decorator using Scrutor (`Decorate`).
- Key files:
  - `Ludamo.Cache.SourceGenerator/CacheDecoratorGenerator.cs` — scanning logic, predicate/transform and registration of generated sources.
  - `Ludamo.Cache.SourceGenerator/SourceGenerationHelper.cs` — template strings and helpers used to produce the generated code.
  - `Ludamo.Cache.SourceGenerator/WrappedInfo.cs` — the model describing what is emitted for each decorated type/method.
  - `Ludamo.Cache.SourceGenerator/CacheDecoratedAttribute.cs` — source for the attribute is emitted at post-init; attribute namespace constants live in `SourceGenerationHelper`.
  - `Ludamo.Cache.SourceGenerator.Demo.Console/` — a tiny console demo (see `Program.cs` and `UserService.cs`) demonstrating generated decorator usage.

## How the generator detects and transforms code

- The generator looks at every `MethodDeclarationSyntax` and checks for attributes named `CacheDecorated` or `CacheDecoratedAttribute`.
- It validates the attribute by comparing the attribute symbol against the two constants in `SourceGenerationHelper` (`AttributeNamespace` and `FullAttributeNamespace`).
- It collects method details into `WrappedInfo` (namespace, name, interface, methods[]). The generator:
  - Detects the first parameter as the cache key (if any). If no parameters, it uses the `ClassName.MethodName` string as cache key.
  - Handles async methods by inspecting the generic type parameter of the returned `Task<T>` and setting `SpecificReturnType` accordingly.
  - Reads an optional `expiresInSeconds` constructor argument from the attribute to emit `MemoryCacheEntryOptions`.

## Output and DI conventions (practical examples)

- Generated file names (examples): `UserServiceCacheDecorator.g.cs`, `UserServiceServiceCollectionExtension.g.cs`.
- Decorator naming: `WrappedInfo.DecoratedName` strips a leading `I` from interfaces (e.g. `IUserService` -> `UserServiceCacheDecorator`) otherwise `{Name}CacheDecorator`.
- Service registration behavior (from `SourceGenerationHelper.GenerateServiceCollectionExtension`):
  - If an interface exists, the generator emits code that registers the concrete and calls `services.Decorate<Interface, Decorator>()` (Scrutor required).
  - If no interface is detected, it uses `TryAddTransient` and transient registrations for the decorator.

## Where to inspect generated output

- Build the solution and inspect the project's `obj` directory to see generated `.g.cs` files: 
  - `Ludamo.Cache.SourceGenerator/obj/Debug/netstandard2.0/` (or similar TFM/config).
- The generator also emits the attribute source at post-init (`CacheDecoratedAttribute.g.cs`) — this is added to compilations automatically.

## Build / run / debug (developer workflows)

Use dotnet CLI from the repository root (PowerShell shown):

```powershell
# build the entire solution
dotnet build .\Ludamo.CacheSourceGenerator\Ludamo.CacheSourceGenerator.slnx

# run the demo console (will compile generator and demo and run the demo)
dotnet run --project .\Ludamo.CacheSourceGenerator\Ludamo.Cache.SourceGenerator.Demo.Console\Ludamo.Cache.SourceGenerator.Demo.Console.csproj
```

Notes:
- Rebuilding the solution is the fastest way to force a source generation pass and to inspect generated files in `obj`.
- To step through generator code, open `CacheDecoratorGenerator.cs` in the debugger and run a build of the demo — attach the debugger to the `dotnet` build process or run tests inside an IDE that supports debugging project compilation.

## Project-specific conventions / gotchas

- Attribute matching is strict: the generator only accepts attributes whose symbol equals the constants in `SourceGenerationHelper`. If you rename the attribute or move namespaces, update `AttributeNamespace` and `FullAttributeNamespace`.
- First parameter of a decorated method becomes the cache key. If you need different behavior you must modify `CacheDecoratorGenerator.GetDecoratedToGenerate` and corresponding template builders in `SourceGenerationHelper`.
- Async method handling: generator expects `Task<T>` style async returns and extracts the inner `T` as `SpecificReturnType` to create typed cache lookups.
- The generated service collection code depends on Scrutor (`services.Decorate`). If a consumer project doesn't include Scrutor, the registration code may fail — ensure consumer projects reference `Scrutor` where decorators are used.

## Quick pointers for edits

- To change templates, edit `SourceGenerationHelper` (string templates & builder methods). Keep templates simple string builders as the project currently uses them.
- To change scan logic, edit `CacheDecoratorGenerator` (Predicate & Transform methods).

## Where to look next / links

- `Ludamo.Cache.SourceGenerator/CacheDecoratorGenerator.cs` — scanning + registration logic
- `Ludamo.Cache.SourceGenerator/SourceGenerationHelper.cs` — templates & emitted code samples
- `Ludamo.Cache.SourceGenerator.Demo.Console/UserService.cs` — canonical usage example of `[CacheDecorated]`

---
If any of these areas should be expanded (examples, more command variants, debugging tips), tell me which part you'd like me to elaborate on and I'll iterate.
