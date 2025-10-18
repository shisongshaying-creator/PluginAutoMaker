param(
    [string]$Requirement = "プラグイン名: HelloGUI /hello コマンドで 'Hello' を返す"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'tools/PluginAutoMaker.ConsoleRunner/PluginAutoMaker.ConsoleRunner.csproj'

Write-Host "要件:" -ForegroundColor Cyan
Write-Host $Requirement -ForegroundColor Yellow

if (-not (Test-Path $project)) {
    throw "Console runner project not found: $project"
}

& dotnet run --project $project -- $Requirement
