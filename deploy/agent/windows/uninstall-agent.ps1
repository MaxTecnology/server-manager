[CmdletBinding()]
param(
    [string]$ServiceName = "SessionManagerAgent",
    [string]$InstallPath = "C:\\Program Files\\SessionManagerAgent",
    [switch]$RemoveData
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

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    if ($service.Status -ne "Stopped") {
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
    }

    & sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 1
}

if (Test-Path $InstallPath) {
    Remove-Item -Path $InstallPath -Recurse -Force
}

if ($RemoveData) {
    $dataRoot = Join-Path $env:ProgramData "SessionManagerAgent"
    if (Test-Path $dataRoot) {
        Remove-Item -Path $dataRoot -Recurse -Force
    }
}

Write-Host "Desinstalacao concluida."
