# Compilation fix — 1.0.2.0

The project now enables:

```xml
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
```

It also includes explicit `System` and `System.Collections.Generic` imports in
the files that use `Guid`, `TimeSpan`, `IReadOnlyList<T>`, and
`IEnumerable<T>`.

Build the project outside Jellyfin's plugin installation directory:

```powershell
cd G:\.gitClones\Jellyfin-Path-Hider-Plugin
.\build-release.ps1
```

The generated release ZIP contains only:

```text
Jellyfin.Plugin.PathHider.dll
```
