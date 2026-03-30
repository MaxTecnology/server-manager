# Operacao Local do Agent Windows (MVP)

Ultima atualizacao: 2026-03-29

## Objetivo deste guia

Padronizar operacao local do MVP do Agent Windows:

1. subir stack local completa (Postgres + API + Front)
2. validar endpoints do Agent
3. publicar e instalar o Agent Windows como servico
4. validar ciclo heartbeat -> snapshot -> `/sessions` e dashboard

## 1) Subir stack local (WSL)

Opcional: copie `.env.local.example` para `.env.local` para customizar senha/admin/chaves no ambiente local.

```bash
docker compose --env-file .env.local -f docker-compose.local.yml up --build -d
docker compose --env-file .env.local -f docker-compose.local.yml ps
```

Servicos esperados:

- `postgres` healthy
- `sessionmanager-api` up
- `sessionmanager-front` up

Endpoints locais:

- API: `http://localhost:${SESSIONMANAGER_API_PORT:-5000}`
- Front: `http://localhost:${SESSIONMANAGER_FRONT_PORT:-8080}`

## 2) Validacao rapida da aplicacao

```bash
docker run --rm --network host curlimages/curl:8.7.1 -sS http://localhost:${SESSIONMANAGER_API_PORT:-5000}/api/health
docker run --rm --network host curlimages/curl:8.7.1 -sSI http://localhost:${SESSIONMANAGER_FRONT_PORT:-8080}/
```

Esperado:

- health `{"status":"ok"...}`
- frontend com `HTTP/1.1 200 OK`

## 3) Credenciais locais padrao

- admin: `admin`
- senha: `Admin@12345`
- agent key: `DEV_ONLY_AGENT_KEY_CHANGE_ME`

## 4) Fluxo de API do Agent (sem servico)

### 4.1 Heartbeat

```bash
curl -X POST http://localhost:${SESSIONMANAGER_API_PORT:-5000}/api/agent/heartbeat \
  -H "X-Agent-Key: DEV_ONLY_AGENT_KEY_CHANGE_ME" \
  -H "Content-Type: application/json" \
  -d '{"serverName":"WSL-RDS","hostname":"WSL-RDS","agentId":"agent-windows-01","agentVersion":"0.1.0","supportsRds":true,"supportsAd":false}'
```

### 4.2 Enviar snapshot de sessoes

```bash
curl -X POST http://localhost:${SESSIONMANAGER_API_PORT:-5000}/api/agent/session-snapshot \
  -H "X-Agent-Key: DEV_ONLY_AGENT_KEY_CHANGE_ME" \
  -H "Content-Type: application/json" \
  -d '{"serverName":"WSL-RDS","hostname":"WSL-RDS","agentId":"agent-windows-01","agentVersion":"0.1.0","supportsRds":true,"supportsAd":false,"capturedAtUtc":"2026-03-29T19:40:45Z","sessionsOutput":" USERNAME              SESSIONNAME        ID  STATE   IDLE TIME  LOGON TIME\n>admin                 rdp-tcp#1          3   Active      none   3/29/2026 10:00 AM"}'
```

### 4.2-b Enviar snapshot de OUs AD (quando `SupportsAd=true`)

```bash
curl -X POST http://localhost:${SESSIONMANAGER_API_PORT:-5000}/api/agent/ad-ou-snapshot \
  -H "X-Agent-Key: DEV_ONLY_AGENT_KEY_CHANGE_ME" \
  -H "Content-Type: application/json" \
  -d '{"serverName":"WSL-AD","hostname":"WSL-AD","agentId":"agent-windows-01","agentVersion":"0.1.0","supportsRds":false,"supportsAd":true,"capturedAtUtc":"2026-03-30T00:30:00Z","organizationalUnitsOutput":"[{\"name\":\"Usuarios\",\"distinguishedName\":\"OU=Usuarios,DC=empresa,DC=local\",\"canonicalName\":\"empresa.local/Usuarios\"}]"}'
```

### 4.3 Login admin

```bash
curl -X POST http://localhost:${SESSIONMANAGER_API_PORT:-5000}/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@12345"}'
```

Copie `accessToken`.

### 4.4 Validar leitura no frontend/API

```bash
curl "http://localhost:${SESSIONMANAGER_API_PORT:-5000}/api/sessions?serverName=WSL-RDS" \
  -H "Authorization: Bearer {accessToken}"

curl "http://localhost:${SESSIONMANAGER_API_PORT:-5000}/api/dashboard/metrics" \
  -H "Authorization: Bearer {accessToken}"
```

### 4.5 Fluxo de comando (opcional)

Se quiser validar tambem a fila de comandos administrativos:

```bash
curl -X POST http://localhost:${SESSIONMANAGER_API_PORT:-5000}/api/agent-commands/servers/{serverId}/commands \
  -H "Authorization: Bearer {accessToken}" \
  -H "Content-Type: application/json" \
  -d '{"commandText":"echo agent-mvp-ok"}'

curl -X POST http://localhost:${SESSIONMANAGER_API_PORT:-5000}/api/agent/next-command \
  -H "X-Agent-Key: DEV_ONLY_AGENT_KEY_CHANGE_ME" \
  -H "Content-Type: application/json" \
  -d '{"hostname":"WSL-RDS","agentId":"agent-windows-01"}'

curl -X POST http://localhost:${SESSIONMANAGER_API_PORT:-5000}/api/agent/commands/{commandId}/result \
  -H "X-Agent-Key: DEV_ONLY_AGENT_KEY_CHANGE_ME" \
  -H "Content-Type: application/json" \
  -d '{"success":true,"resultOutput":"agent-mvp-ok"}'
```

### 4.6 Conferir auditoria

```bash
curl "http://localhost:${SESSIONMANAGER_API_PORT:-5000}/api/audit?search=AGENT&page=1&pageSize=20" \
  -H "Authorization: Bearer {accessToken}"
```

## 5) Agent Windows como servico

Scripts em `deploy/agent/windows/`:

- `publish-agent.ps1`
- `install-agent.ps1`
- `update-agent.ps1`
- `uninstall-agent.ps1`

### 5.1 Publicar binario do agent

No repositorio:

```powershell
pwsh -File .\deploy\agent\windows\publish-agent.ps1 `
  -Configuration Release `
  -Runtime win-x64 `
  -OutputPath .\publish\agent-win-x64
```

### 5.2 Instalar no Windows Server

No Windows Server (PowerShell como Administrador):

```powershell
.\install-agent.ps1 `
  -ApiBaseUrl "http://SEU_API:5000" `
  -ApiKey "DEV_ONLY_AGENT_KEY_CHANGE_ME" `
  -ServerName "SRV-RDS-01" `
  -AgentId "agent-srv-rds-01" `
  -AdOuSnapshotIntervalSeconds 300 `
  -SupportsRds $true `
  -SupportsAd $false
```

Observacoes:

- se API estiver no mesmo host do agent, pode usar `http://localhost:5000`
- para Dockploy/producao, usar URL final de API (`https://api.seu-dominio.com`)
- `ApiKey` deve ser igual a `Agent:ApiKey` da API
- `AdOuSnapshotIntervalSeconds` controla a frequencia de envio das OUs AD (quando `SupportsAd=true`)
- perfis recomendados:
  - servidor RDS: `-SupportsRds $true -SupportsAd $false`
  - servidor AD: `-SupportsRds $false -SupportsAd $true`
  - servidor misto: `-SupportsRds $true -SupportsAd $true`

Servico criado:

- nome: `SessionManagerAgent`
- install path: `C:\Program Files\SessionManagerAgent`
- config: `C:\Program Files\SessionManagerAgent\appsettings.Production.json`
- fila local de retry: `C:\ProgramData\SessionManagerAgent\data\pending-results.json`

### 5.3 Validar servico

```powershell
Get-Service SessionManagerAgent
sc.exe query SessionManagerAgent
```

Conferir logs:

- Event Viewer -> `Windows Logs` -> `Application`
- Source: `SessionManagerAgent`

### 5.4 Atualizacao

```powershell
.\update-agent.ps1 `
  -PublishPath .\publish\agent-win-x64 `
  -InstallPath "C:\Program Files\SessionManagerAgent"
```

### 5.5 Desinstalacao

```powershell
.\uninstall-agent.ps1 -ServiceName "SessionManagerAgent"
```

Remover tambem dados pendentes:

```powershell
.\uninstall-agent.ps1 -ServiceName "SessionManagerAgent" -RemoveData
```

## 6) Troubleshooting rapido

### Erro: `'Import-Module' is not recognized as an internal or external command`

Causa comum:

- agent desatualizado executando comando AD em `cmd.exe` em vez de `powershell.exe`

Correcao:

```powershell
# pasta extraida do pacote mais recente
Stop-Service SessionManagerAgent

.\package\update-agent.ps1 `
  -PublishPath ".\agent-win-x64" `
  -InstallPath "C:\Program Files\SessionManagerAgent" `
  -ServiceName "SessionManagerAgent"

Restart-Service SessionManagerAgent
Get-Service SessionManagerAgent
```

Validacao:

- reenfileirar o comando AD pelo frontend (`/active-directory`)
- confirmar `Succeeded` no ultimo comando AD

### Erro ao carregar OUs no frontend AD

Mensagem comum:

- `Snapshot de OUs desatualizado. Verifique o agent AD.`

Checklist:

1. confirmar que o agent AD foi instalado com `-SupportsAd $true`
2. verificar heartbeat recente do agente
3. revisar log do `SessionManagerAgent` no Event Viewer para falha no comando `Get-ADOrganizationalUnit`
