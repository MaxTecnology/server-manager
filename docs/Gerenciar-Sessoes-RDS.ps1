#requires -version 5.1
<#
    Gerenciar-Sessoes-RDS.ps1
    Menu simples para suporte operacional em Windows Server / RDS

    Funções:
    - Listar sessões
    - Desconectar sessão (rwinsta)
    - Fazer logoff de sessão
    - Encerrar processo por nome em uma sessão
    - Registrar log local

    Observação:
    - Execute como administrador
    - Ajuste o nome do servidor abaixo se quiser apontar para um host específico
#>

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$ServerName = $env:COMPUTERNAME
$LogFile = "C:\Logs\Gerenciar-Sessoes-RDS.log"

function Ensure-LogPath {
    $dir = Split-Path $LogFile -Parent
    if (-not (Test-Path $dir)) {
        New-Item -Path $dir -ItemType Directory -Force | Out-Null
    }
}

function Write-Log {
    param(
        [string]$Action,
        [string]$Details
    )

    Ensure-LogPath
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $operator = "$env:USERDOMAIN\$env:USERNAME"
    $line = "$timestamp | Operador: $operator | Ação: $Action | Detalhes: $Details"
    Add-Content -Path $LogFile -Value $line
}

function Test-Admin {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-Sessions {
    param(
        [string]$Server = $ServerName
    )

    $raw = cmd /c "query user /server:$Server" 2>$null
    if (-not $raw) {
        Write-Host ""
        Write-Host "Não foi possível consultar sessões no servidor $Server." -ForegroundColor Red
        return @()
    }

    $sessions = @()

    foreach ($line in $raw) {
        if ($line -match "USERNAME\s+SESSIONNAME\s+ID\s+STATE\s+IDLE TIME\s+LOGON TIME") {
            continue
        }

        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        # Remove o caractere ">" da sessão atual, se existir
        $clean = $line -replace "^\s*>", ""
        $clean = $clean -replace "^\s+", ""

        # Tentativa de parse tolerante
        $pattern = '^(?<USERNAME>\S+)?\s+(?<SESSIONNAME>\S+)?\s+(?<ID>\d+)\s+(?<STATE>Active|Disc|Listen|Idle|Down|Conn)\s+(?<IDLETIME>.+?)\s+(?<LOGONTIME>\d{1,2}/\d{1,2}/\d{4}.+)$'
        $m = [regex]::Match($clean, $pattern)

        if ($m.Success) {
            $sessions += [pscustomobject]@{
                UserName    = $m.Groups["USERNAME"].Value.Trim()
                SessionName = $m.Groups["SESSIONNAME"].Value.Trim()
                Id          = [int]$m.Groups["ID"].Value.Trim()
                State       = $m.Groups["STATE"].Value.Trim()
                IdleTime    = $m.Groups["IDLETIME"].Value.Trim()
                LogonTime   = $m.Groups["LOGONTIME"].Value.Trim()
            }
            continue
        }

        # Fallback: split por múltiplos espaços
        $parts = $clean -split '\s{2,}'
        if ($parts.Count -ge 5) {
            $idIndex = -1
            for ($i = 0; $i -lt $parts.Count; $i++) {
                if ($parts[$i] -match '^\d+$') {
                    $idIndex = $i
                    break
                }
            }

            if ($idIndex -ge 0 -and ($idIndex + 2) -lt $parts.Count) {
                $user = if ($idIndex -ge 1) { $parts[0].Trim() } else { "" }
                $sessionName = if ($idIndex -ge 2) { $parts[1].Trim() } else { "" }
                $id = [int]$parts[$idIndex].Trim()
                $state = $parts[$idIndex + 1].Trim()
                $idle = if (($idIndex + 2) -lt $parts.Count) { $parts[$idIndex + 2].Trim() } else { "" }
                $logon = if (($idIndex + 3) -lt $parts.Count) { ($parts[($idIndex + 3)..($parts.Count - 1)] -join " ").Trim() } else { "" }

                $sessions += [pscustomobject]@{
                    UserName    = $user
                    SessionName = $sessionName
                    Id          = $id
                    State       = $state
                    IdleTime    = $idle
                    LogonTime   = $logon
                }
            }
        }
    }

    return $sessions | Sort-Object Id
}

function Show-Sessions {
    $sessions = Get-Sessions
    Write-Host ""
    if (-not $sessions -or $sessions.Count -eq 0) {
        Write-Host "Nenhuma sessão encontrada." -ForegroundColor Yellow
        return
    }

    $sessions | Format-Table Id, UserName, SessionName, State, IdleTime, LogonTime -AutoSize
}

function Disconnect-Session {
    param([int]$SessionId)

    # rwinsta é o utilitário clássico para resetar/desconectar sessão
    $result = cmd /c "rwinsta $SessionId /server:$ServerName" 2>&1
    $code = $LASTEXITCODE

    if ($code -eq 0) {
        Write-Host "Sessão $SessionId desconectada com sucesso." -ForegroundColor Green
        Write-Log -Action "DISCONNECT" -Details "Sessão $SessionId desconectada. Servidor: $ServerName"
    } else {
        Write-Host "Falha ao desconectar sessão $SessionId." -ForegroundColor Red
        if ($result) { Write-Host $result -ForegroundColor DarkRed }
        Write-Log -Action "DISCONNECT_ERROR" -Details "Erro ao desconectar sessão $SessionId. Saída: $result"
    }
}

function Logoff-Session {
    param([int]$SessionId)

    $result = cmd /c "logoff $SessionId /server:$ServerName" 2>&1
    $code = $LASTEXITCODE

    if ($code -eq 0) {
        Write-Host "Logoff da sessão $SessionId realizado com sucesso." -ForegroundColor Green
        Write-Log -Action "LOGOFF" -Details "Sessão $SessionId encerrada com logoff. Servidor: $ServerName"
    } else {
        Write-Host "Falha ao fazer logoff da sessão $SessionId." -ForegroundColor Red
        if ($result) { Write-Host $result -ForegroundColor DarkRed }
        Write-Log -Action "LOGOFF_ERROR" -Details "Erro ao fazer logoff da sessão $SessionId. Saída: $result"
    }
}

function Stop-ProcessInSession {
    param(
        [int]$SessionId,
        [string]$ImageName
    )

    if (-not $ImageName.EndsWith(".exe", [System.StringComparison]::OrdinalIgnoreCase)) {
        $ImageName = "$ImageName.exe"
    }

    $cmdLine = "taskkill /server $ServerName /FI `"SESSION eq $SessionId`" /IM `"$ImageName`" /F"
    $result = cmd /c $cmdLine 2>&1
    $code = $LASTEXITCODE

    if ($code -eq 0) {
        Write-Host "Processo $ImageName encerrado na sessão $SessionId." -ForegroundColor Green
        Write-Log -Action "TASKKILL" -Details "Processo $ImageName encerrado na sessão $SessionId. Servidor: $ServerName"
    } else {
        Write-Host "Falha ao encerrar $ImageName na sessão $SessionId." -ForegroundColor Red
        if ($result) { Write-Host $result -ForegroundColor DarkRed }
        Write-Log -Action "TASKKILL_ERROR" -Details "Erro ao encerrar $ImageName na sessão $SessionId. Saída: $result"
    }
}

function Show-Header {
    Clear-Host
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "   GERENCIADOR DE SESSÕES RDS / REMOTEAPP " -ForegroundColor Cyan
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "Servidor: $ServerName"
    Write-Host "Log:      $LogFile"
    Write-Host ""
}

function Pause-Step {
    Write-Host ""
    Read-Host "Pressione ENTER para continuar"
}

if (-not (Test-Admin)) {
    Write-Host "Execute este script como Administrador." -ForegroundColor Red
    exit 1
}

do {
    Show-Header
    Write-Host "1 - Listar sessões"
    Write-Host "2 - Desconectar sessão"
    Write-Host "3 - Fazer logoff da sessão"
    Write-Host "4 - Encerrar processo em uma sessão"
    Write-Host "5 - Alterar servidor alvo"
    Write-Host "0 - Sair"
    Write-Host ""

    $option = Read-Host "Escolha uma opção"

    switch ($option) {
        "1" {
            Show-Sessions
            Pause-Step
        }

        "2" {
            Show-Sessions
            Write-Host ""
            $id = Read-Host "Informe o ID da sessão para desconectar"
            if ($id -match '^\d+$') {
                $confirm = Read-Host "Confirmar desconexão da sessão $id? (S/N)"
                if ($confirm -match '^(S|s)$') {
                    Disconnect-Session -SessionId ([int]$id)
                }
            } else {
                Write-Host "ID inválido." -ForegroundColor Yellow
            }
            Pause-Step
        }

        "3" {
            Show-Sessions
            Write-Host ""
            $id = Read-Host "Informe o ID da sessão para fazer logoff"
            if ($id -match '^\d+$') {
                Write-Host "Atenção: logoff encerra a sessão e pode causar perda de trabalho não salvo." -ForegroundColor Yellow
                $confirm = Read-Host "Confirmar logoff da sessão $id? (S/N)"
                if ($confirm -match '^(S|s)$') {
                    Logoff-Session -SessionId ([int]$id)
                }
            } else {
                Write-Host "ID inválido." -ForegroundColor Yellow
            }
            Pause-Step
        }

        "4" {
            Show-Sessions
            Write-Host ""
            $id = Read-Host "Informe o ID da sessão"
            $proc = Read-Host "Informe o nome do processo (ex.: excel.exe ou winword)"
            if ($id -match '^\d+$' -and -not [string]::IsNullOrWhiteSpace($proc)) {
                $confirm = Read-Host "Confirmar encerramento do processo '$proc' na sessão $id? (S/N)"
                if ($confirm -match '^(S|s)$') {
                    Stop-ProcessInSession -SessionId ([int]$id) -ImageName $proc
                }
            } else {
                Write-Host "Dados inválidos." -ForegroundColor Yellow
            }
            Pause-Step
        }

        "5" {
            $newServer = Read-Host "Digite o nome do servidor"
            if (-not [string]::IsNullOrWhiteSpace($newServer)) {
                $ServerName = $newServer.Trim()
                Write-Log -Action "CHANGE_SERVER" -Details "Servidor alterado para $ServerName"
                Write-Host "Servidor alterado para $ServerName" -ForegroundColor Green
            }
            Pause-Step
        }

        "0" {
            Write-Host "Saindo..."
        }

        default {
            Write-Host "Opção inválida." -ForegroundColor Yellow
            Pause-Step
        }
    }

} while ($option -ne "0")