param([Parameter(Mandatory = $true)][string]$DalamudPath)
$ErrorActionPreference = 'Stop'
$resolved = (Resolve-Path -LiteralPath $DalamudPath).Path
foreach ($file in @('Dalamud.dll', 'Dalamud.Bindings.ImGui.dll', 'FFXIVClientStructs.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $resolved $file))) { throw "Missing $file in the Dalamud directory." }
}
$previousDalamud = $env:DALAMUD_HOME
$env:DALAMUD_HOME = $resolved
Push-Location $PSScriptRoot
try {
    dotnet restore XLToolbar.csproj --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Dependency restore failed.' }
    dotnet build XLToolbar.csproj -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed; use Dalamud API 15.' }
    dotnet run --project tests/Layout/Layout.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Layout tests failed.' }
    Write-Host "Plugin DLL: $(Join-Path $PSScriptRoot 'bin\Release\XLToolbar.dll')"
} finally { Pop-Location; $env:DALAMUD_HOME = $previousDalamud }
