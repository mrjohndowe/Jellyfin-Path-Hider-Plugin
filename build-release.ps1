param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src\Jellyfin.Plugin.PathHider\Jellyfin.Plugin.PathHider.csproj"
$artifacts = Join-Path $root "artifacts"
$staging = Join-Path $root "package"
$zip = Join-Path $root "Jellyfin.Plugin.PathHider_1.0.2.0.zip"

Remove-Item $artifacts -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $zip -Force -ErrorAction SilentlyContinue

dotnet clean $project --configuration $Configuration
dotnet publish $project --configuration $Configuration --output $artifacts

New-Item $staging -ItemType Directory -Force | Out-Null
Copy-Item (Join-Path $artifacts "Jellyfin.Plugin.PathHider.dll") $staging

Compress-Archive `
    -Path (Join-Path $staging "Jellyfin.Plugin.PathHider.dll") `
    -DestinationPath $zip `
    -Force

Write-Host ""
Write-Host "Release package created:"
Write-Host $zip
Write-Host ""
Write-Host "ZIP contents:"
tar -tf $zip
