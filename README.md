# Jellyfin Path Hider

A Jellyfin server plugin that excludes configured files and folders from library scans.

The plugin is non-destructive: it does not delete, move, rename, or change permissions on media.

## Compatibility baseline

This source tree targets:

- Jellyfin server API packages `10.10.7`
- .NET `8.0`
- Plugin target ABI `10.10.0.0`

Jellyfin plugin APIs and ABI requirements can change. If your server uses another release line,
update the two Jellyfin package versions in
`src/Jellyfin.Plugin.PathHider/Jellyfin.Plugin.PathHider.csproj`,
the `TargetFramework` if required, and `targetAbi` in `build.yaml`.

## Rule syntax

Enter one rule per line in **Dashboard → Plugins → Path Hider**.

```text
# Hide one folder by absolute path
folder:/media/private

# Hide any folder named Extras, regardless of depth
folder:Extras

# Hide workprint files anywhere below a library
file:**/*-workprint.mkv

# Hide one exact file
file:/media/movies/Example/rough-cut.mkv

# Windows paths are accepted
folder:D:\Media\Private
```

Rules support:

- `file:` — files only
- `folder:` — directories only
- `any:` — either type; this is also the default
- `*` — zero or more characters except a path separator
- `?` — exactly one character except a path separator
- `**` — zero or more characters, including path separators
- `#` at the start of a line — comment
- Single or double quotes around paths containing spaces

A pattern containing `/` or `\` is matched against the normalized full path.
A pattern with no path separator is matched against the entry name.

## Build

Install the .NET 8 SDK, then run:

```bash
dotnet restore
dotnet test
dotnet publish src/Jellyfin.Plugin.PathHider/Jellyfin.Plugin.PathHider.csproj \
  --configuration Release \
  --output ./artifacts
```

## Install manually

1. Stop Jellyfin.
2. Create a plugin directory named `Path Hider` under Jellyfin's plugin directory.
3. Copy `artifacts/Jellyfin.Plugin.PathHider.dll` into that directory.
4. Start Jellyfin.
5. Open **Dashboard → Plugins → Path Hider**, enter rules, and save.
6. Run a full library scan.

Common plugin directory locations include:

- Linux packages: `/var/lib/jellyfin/plugins/Path Hider/`
- Docker: the `plugins` directory inside the mounted Jellyfin config volume
- Windows: `%ProgramData%\Jellyfin\Server\plugins\Path Hider\`

Use the plugin directory configured for your installation if it differs.

## Behavior and limitations

- Filtering occurs when Jellyfin resolves filesystem entries during a library scan.
- Existing database items may remain visible until Jellyfin completes a full scan and reconciles the library.
- Rules affect Jellyfin only; they do not hide paths from the operating system, network shares, or other applications.
- A rule must match the path as Jellyfin sees it inside the server/container. For Docker, use container paths such as `/media/...`, not host-only paths.
- Symbolic links, bind mounts, and case behavior can make the observed path differ from the host path.
- Enable match logging temporarily when diagnosing rules; disable it afterward to avoid noisy logs.

## Development notes

The plugin registers an `IResolverIgnoreRule`. The matcher is cached and rebuilt only when the
rule text or case-sensitivity setting changes. Generated regular expressions have a timeout and
do not accept arbitrary regular-expression syntax.
