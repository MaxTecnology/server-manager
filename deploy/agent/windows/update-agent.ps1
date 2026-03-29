[CmdletBinding()]
param(
    [string]$PublishPath = ".\\publish\\agent-win-x64",
    [string]$InstallPath = "C:\\Program Files\\SessionManagerAgent",
    [string]$ServiceName = "SessionManagerAgent"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Execute este script em PowerShell como Administrador."
    }
}

Assert-Administrator

if (-not (Test-Path $PublishPath)) {
    throw "PublishPath nao encontrado: $PublishPath"
}

if (-not (Test-Path $InstallPath)) {
    throw "InstallPath nao encontrado: $InstallPath"
}

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    throw "Servico nao encontrado: $ServiceName"
}

if ($service.Status -ne "Stopped") {
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 1
}

& robocopy $PublishPath $InstallPath /E /R:2 /W:1 /NFL /NDL /NJH /NJS /NP /XF appsettings.json appsettings.Development.json appsettings.Production.json | Out-Null
if ($LASTEXITCODE -gt 7) {
    throw "Robocopy falhou com codigo $LASTEXITCODE"
}

Start-Service -Name $ServiceName
Start-Sleep -Seconds 1
Get-Service -Name $ServiceName | Format-Table -AutoSize
Write-Host "Atualizacao concluida."
