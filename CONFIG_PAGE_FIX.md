# Configuration page fix

Version 1.0.1.0 fixes the missing Jellyfin Settings/configuration page by:

- using a route-safe page name: `PathHiderConfig`;
- assigning an explicit embedded-resource logical name;
- enabling the page in Jellyfin's Dashboard menu.

## Build

```powershell
dotnet publish .\src\Jellyfin.Plugin.PathHider\Jellyfin.Plugin.PathHider.csproj `
  --configuration Release `
  --output .\artifacts
```

Stop Jellyfin, replace the old plugin DLL with the new DLL, remove any duplicate
Path Hider plugin folders, and start Jellyfin again. A browser hard refresh may
also be needed.

The configuration page route is:

```text
/web/index.html#!/configurationpage?name=PathHiderConfig
```
