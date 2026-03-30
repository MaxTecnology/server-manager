[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$ApiKey,

    [string]$PublishPath = ".\\publish\\agent-win-x64",
    [string]$InstallPath = "C:\\Program Files\\SessionManagerAgent",
    [string]$ServiceName = "SessionManagerAgent",
    [string]$DisplayName = "Session Manager Agent",
    [string]$AgentId,
    [string]$ServerName,
    [string]$Hostname,
    [int]$HeartbeatIntervalSeconds = 30,
    [int]$PollIntervalSeconds = 5,
    [int]$CommandTimeoutSeconds = 120,
    [int]$AdOuSnapshotIntervalSeconds = 300,
    [int]$MaxAdOuSnapshotOutputLength = 500000,
    [bool]$SupportsRds = $true,
    [bool]$SupportsAd = $false
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

function Invoke-Robocopy {
    param(
        [string]$Source,
        [string]$Target,
        [string[]]$ExtraArgs
    )

    $args = @($Source, $Target, "/MIR", "/R:2", "/W:1", "/NFL", "/NDL", "/NJH", "/NJS", "/NP") + $ExtraArgs
    & robocopy @args | Out-Null
    if ($LASTEXITCODE -gt 7) {
        throw "Robocopy falhou com codigo $LASTEXITCODE"
    }
}

Assert-Administrator

if (-not (Test-Path $PublishPath)) {
    throw "PublishPath nao encontrado: $PublishPath"
}

if (-not $SupportsRds -and -not $SupportsAd) {
    throw "Informe ao menos uma capacidade: SupportsRds ou SupportsAd."
}

$resolvedAgentId = if ([string]::IsNullOrWhiteSpace($AgentId)) { "$env:COMPUTERNAME-agent" } else { $AgentId.Trim() }
$resolvedServerName = if ([string]::IsNullOrWhiteSpace($ServerName)) { $env:COMPUTERNAME } else { $ServerName.Trim() }
$resolvedHostname = if ([string]::IsNullOrWhiteSpace($Hostname)) { $resolvedServerName } else { $Hostname.Trim() }
$dataDirectory = Join-Path $env:ProgramData "SessionManagerAgent\\data"

New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null
New-Item -ItemType Directory -Force -Path $dataDirectory | Out-Null

$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    if ($existingService.Status -ne "Stopped") {
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
    }

    & sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 1
}

Invoke-Robocopy -Source $PublishPath -Target $InstallPath -ExtraArgs @()

$config = @{
    Agent = @{
        ApiBaseUrl = $ApiBaseUrl
        ApiKey = $ApiKey
        AgentId = $resolvedAgentId
        ServerName = $resolvedServerName
        Hostname = $resolvedHostname
        HeartbeatIntervalSeconds = $HeartbeatIntervalSeconds
        PollIntervalSeconds = $PollIntervalSeconds
        CommandTimeoutSeconds = $CommandTimeoutSeconds
        AdOuSnapshotIntervalSeconds = $AdOuSnapshotIntervalSeconds
        SupportsRds = $SupportsRds
        SupportsAd = $SupportsAd
        DataDirectory = $dataDirectory
        MaxResultOutputLength = 4000
        MaxAdOuSnapshotOutputLength = $MaxAdOuSnapshotOutputLength
    }
    Logging = @{
        LogLevel = @{
            Default = "Information"
            "Microsoft.Hosting.Lifetime" = "Information"
        }
    }
}

$configPath = Join-Path $InstallPath "appsettings.Production.json"
$config | ConvertTo-Json -Depth 10 | Set-Content -Path $configPath -Encoding UTF8

$exePath = Join-Path $InstallPath "SessionManager.Agent.Windows.exe"
if (-not (Test-Path $exePath)) {
    throw "Executavel nao encontrado em: $exePath"
}

$binaryPath = "`"$exePath`" --environment Production"
New-Service -Name $ServiceName -BinaryPathName $binaryPath -DisplayName $DisplayName -StartupType Automatic -Description "Agent Windows do Session Manager" | Out-Null

& sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/5000/restart/5000 | Out-Null
& sc.exe failureflag $ServiceName 1 | Out-Null

Start-Service -Name $ServiceName
Start-Sleep -Seconds 1

Get-Service -Name $ServiceName | Format-Table -AutoSize
Write-Host "Instalacao concluida. Configuracao em: $configPath"
