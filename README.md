# cache-source-generator

This repository contains a small Roslyn incremental source generator that emits cache-decorator wrappers for methods annotated with a custom attribute.

At a glance
- The generator scans source code for methods decorated with `[CacheDecorated]` and emits two artifacts per containing type:
	- `{DecoratedName}CacheDecorator.g.cs` — a partial class that wraps the original type and applies `IMemoryCache` caching logic.
	- `{DecoratedName}ServiceCollectionExtension.g.cs` — an `IServiceCollection` extension that registers the concrete implementation and the decorator (uses Scrutor for `Decorate<TInterface, TDecorator>` when an interface is present).

Why this project exists
- The generator automates the boilerplate of caching responses from service methods. Instead of hand-writing decorator classes that call `IMemoryCache`, add an attribute and rebuild; the generator creates the decorator and the DI wiring.

Key files
- `Ludamo.Cache.SourceGenerator/CacheDecoratorGenerator.cs` — Roslyn incremental generator: syntax predicate, transform logic, and source registration.
- `Ludamo.Cache.SourceGenerator/SourceGenerationHelper.cs` — string templates and helpers used to build the generated `.g.cs` outputs.
- `Ludamo.Cache.SourceGenerator/WrappedInfo.cs` — model describing what will be emitted for each decorated type/method.
- `Ludamo.Cache.SourceGenerator/CacheDecoratedAttribute.cs` — the attribute source (also emitted by the generator at post-init). See `SourceGenerationHelper.CacheDecoratedAttribute` for the canonical emitted source.
- `Ludamo.Cache.SourceGenerator.Demo.Console/` — demo showing how generated decorators are used (`Program.cs`, `UserService.cs`).

How the generator detects and transforms code
- It looks at every `MethodDeclarationSyntax` and checks attribute names `CacheDecorated` or `CacheDecoratedAttribute`.
- It validates the attribute symbol against `SourceGenerationHelper.AttributeNamespace` and `SourceGenerationHelper.FullAttributeNamespace` to avoid false positives.
- It gathers method details into `WrappedInfo` including:
	- containing `Namespace` and `Name` (class/interface),
	- `InterfaceName` if the containing type implements an interface that declares the method,
	- each method's `MethodName`, `ReturnType`, whether `IsAsync`, the first parameter (used as cache key), and optional `ExpiresInSeconds` read from the attribute's constructor arg.

Cache key rules and async handling
- If a method has at least one parameter, the first parameter is used as the cache key. If a method has no parameters, the generator uses the string `ClassName.MethodName` as the key.
- For async methods returning `Task<T>`, the generator extracts the inner `T` and uses it as the typed cache lookup type.

DI / registration conventions
- The generator emits a service registration extension. Behavior depends on whether an interface is present:
	- If an interface exists that exposes the method, the code registers the concrete and calls `services.Decorate<Interface, Decorator>()`. This requires the consumer to reference `Scrutor`.
	- If no interface is detected, it falls back to `TryAddTransient` registration and adds the decorator as a transient service.

Quick start (build & run demo)
From the repository root (PowerShell):

```powershell
# Build the entire solution
dotnet build .\Ludamo.CacheSourceGenerator\Ludamo.CacheSourceGenerator.slnx

# Run the demo console (this will trigger source generation and run the demo)
dotnet run --project .\Ludamo.CacheSourceGenerator\Ludamo.Cache.SourceGenerator.Demo.Console\Ludamo.Cache.SourceGenerator.Demo.Console.csproj
```

Where to inspect generated output
- After a successful build, inspect the `obj` folder for generated `.g.cs` files. Example:
	- `Ludamo.Cache.SourceGenerator/obj/Debug/netstandard2.0/` — contains generated decorator and service collection extension sources.
- The generator also emits a `CacheDecoratedAttribute.g.cs` during post-init; this file will be visible in the `obj` build output as well.

Example: using the attribute (from demo)
In `UserService.cs` (demo) the service is defined like this:

```csharp
public partial class UserService : IUserService
{
		[CacheDecorated]
		public async Task<string> GetUsersAsync() { /* ... */ }

		[CacheDecorated(expiresInSeconds: 30)]
		public async Task<User?> GetUserAsync(int id) { /* ... */ }
}

public interface IUserService
{
		Task<string> GetUsersAsync();
		Task<User?> GetUserAsync(int id);
}
```

After building, the generator will produce something like `UserServiceCacheDecorator.g.cs` containing a partial class implementing the same interface or wrapping the concrete type. For no-arg methods the generated cache key is a static string (`UserService.GetUsersAsync`). For methods with a first parameter the parameter value is used as key.

Notes / gotchas
- Attribute matching is strict — if you rename `CacheDecorated` or move it to another namespace, update `SourceGenerationHelper.AttributeNamespace` and `FullAttributeNamespace`.
- The first parameter is assumed to be a valid cache key. If you need composite keys or custom behavior, modify `CacheDecoratorGenerator.GetDecoratedToGenerate` and `SourceGenerationHelper` templates.
- The generated service registration uses `Scrutor` when decorating by interface. If a consumer doesn't reference `Scrutor`, the registration will fail.

Editing tips
- Change templates and emitted code in `SourceGenerationHelper`.
- Change scanning or validation logic in `CacheDecoratorGenerator`.

Contact / contributing
- This is a small demo/source-generator project. If you'd like to extend it (more key strategies, custom cache key attributes, or additional template styles), open an issue or submit a PR with tests.

---
Small, focused README intended to get a developer or an AI agent productive quickly with the generator and demo.