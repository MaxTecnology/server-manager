[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputPath = ".\\publish\\agent-win-x64",
    [switch]$SingleFile = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectPath = "src/SessionManager.Agent.Windows/SessionManager.Agent.Windows.csproj"

$arguments = @(
    "publish",
    $projectPath,
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-o", $OutputPath
)

if ($SingleFile)
{
    $arguments += "/p:PublishSingleFile=true"
    $arguments += "/p:IncludeNativeLibrariesForSelfExtract=true"
}

dotnet @arguments

Write-Host "Agent publicado em: $OutputPath"
