# Runbook (Operacao e Desenvolvimento)

## Pre-requisitos

- .NET SDK 10
- Node.js 22+
- acesso ao host Windows com comandos RDS disponiveis

## Setup inicial

## 1) Restaurar backend

```powershell
dotnet restore src/SessionManager.WebApi/SessionManager.WebApi.csproj
```

## 2) Restaurar frontend

```powershell
cd src/frontend
npm install
```

## 3) Aplicar banco

```powershell
dotnet tool run dotnet-ef database update `
  --project src/SessionManager.Infrastructure/SessionManager.Infrastructure.csproj `
  --startup-project src/SessionManager.WebApi/SessionManager.WebApi.csproj
```

## Rodar em desenvolvimento

## Docker local (WSL)

Opcional: copie `.env.local.example` para `.env.local` para customizar credenciais locais.

```powershell
docker compose --env-file .env.local -f docker-compose.local.yml up --build -d
docker compose --env-file .env.local -f docker-compose.local.yml ps
```

Observacao:

- o `docker-compose.local.yml` usa Postgres em container (`postgres:16-alpine`)
- a API local usa `ConnectionStrings__DefaultConnection` apontando para `Host=postgres`
- portas de host sao configuraveis:
  - `SESSIONMANAGER_POSTGRES_PORT` (default `5432`)
  - `SESSIONMANAGER_API_PORT` (default `5000`)
  - `SESSIONMANAGER_FRONT_PORT` (default `8080`)
- Postgres fica com bind em `127.0.0.1` para evitar exposicao externa
- como a API roda em container Linux no WSL, dados de sessao devem vir do Agent Windows (heartbeat + snapshot)
- sem snapshot recente do agent, `/api/sessions` pode retornar erro de snapshot desatualizado

Health API:

```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/health"
```

Frontend:

```powershell
Invoke-WebRequest -Uri "http://localhost:8080/" -UseBasicParsing
```

## Backend

```powershell
dotnet run --project src/SessionManager.WebApi/SessionManager.WebApi.csproj
```

## Frontend

```powershell
cd src/frontend
npm run dev
```

## Build local

## Backend

```powershell
dotnet build src/SessionManager.WebApi/SessionManager.WebApi.csproj -m:1
```

## Frontend

```powershell
cd src/frontend
npm run build
```

## Smoke test rapido

## Health

```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/health"
```

## Login

```powershell
$body = @{ username = "admin"; password = "Admin@12345" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" -Method Post -ContentType "application/json" -Body $body
```

## Smoke test Agent MVP (local)

```powershell
$agentBody = @{
  serverName = "WSL-RDS"
  hostname = "WSL-RDS"
  agentId = "agent-windows-01"
  agentVersion = "0.1.0"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/agent/heartbeat" `
  -Method Post `
  -ContentType "application/json" `
  -Headers @{ "X-Agent-Key" = "DEV_ONLY_AGENT_KEY_CHANGE_ME" } `
  -Body $agentBody

$snapshotBody = @{
  serverName = "WSL-RDS"
  hostname = "WSL-RDS"
  agentId = "agent-windows-01"
  agentVersion = "0.1.0"
  capturedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
  sessionsOutput = " USERNAME              SESSIONNAME        ID  STATE   IDLE TIME  LOGON TIME`n>admin                 rdp-tcp#1          3   Active      none   3/29/2026 10:00 AM"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/agent/session-snapshot" `
  -Method Post `
  -ContentType "application/json" `
  -Headers @{ "X-Agent-Key" = "DEV_ONLY_AGENT_KEY_CHANGE_ME" } `
  -Body $snapshotBody
```

## Publicacao para servidor

## 1) Build frontend

```powershell
cd src/frontend
npm ci
npm run build
```

## 2) Publish backend

```powershell
dotnet publish src/SessionManager.WebApi/SessionManager.WebApi.csproj -c Release -o .\\publish
```

## 3) Execucao

- pode rodar como servico Windows ou IIS
- validar permissao da identidade de execucao para comandos RDS
- ajustar `appsettings`/variaveis para ambiente real

## Checklist de validacao pos-deploy

1. `GET /api/health` responde `ok`
2. login com admin funciona
3. dashboard carrega metricas
4. listagem de sessoes funciona para servidor com heartbeat + snapshot recente
5. acoes de sessao geram entradas em auditoria
6. frontend abre via app publicada (se `dist` presente)

## Instalar como servico no Windows

Exemplo considerando artefatos em `C:\Apps\SessionManager\publish`:

```powershell
sc.exe create SessionManagerWebApi `
  binPath= "\"C:\Apps\SessionManager\publish\SessionManager.WebApi.exe\" --urls http://0.0.0.0:5000" `
  start= auto `
  obj= "LocalSystem"
```

Configurar reinicio automatico e iniciar:

```powershell
sc.exe failure SessionManagerWebApi reset= 86400 actions= restart/5000/restart/5000/restart/5000
sc.exe start SessionManagerWebApi
```

Operacao diaria:

```powershell
sc.exe query SessionManagerWebApi
sc.exe stop SessionManagerWebApi
sc.exe start SessionManagerWebApi
```

Se precisar remover e recriar:

```powershell
sc.exe stop SessionManagerWebApi
sc.exe delete SessionManagerWebApi
```

Notas:

- a identidade do servico precisa privilegios para os comandos RDS (`query user`, `rwinsta`, `logoff`, `taskkill`)
- para execucao sem Docker, a API usa SQLite por padrao (arquivo em `./data`)

## Agent Windows (servico)

Publicar (na maquina de build):

```powershell
pwsh -File .\deploy\agent\windows\publish-agent.ps1 `
  -Configuration Release `
  -Runtime win-x64 `
  -OutputPath .\publish\agent-win-x64
```

Instalar no Windows Server (PowerShell admin):

```powershell
.\deploy\agent\windows\install-agent.ps1 `
  -ApiBaseUrl "https://api.seu-dominio.com" `
  -ApiKey "<AgentApiKey>" `
  -ServerName "SRV-RDS-01" `
  -AgentId "agent-srv-rds-01" `
  -AdOuSnapshotIntervalSeconds 300 `
  -SupportsRds $true `
  -SupportsAd $false
```

Operacao:

```powershell
Get-Service SessionManagerAgent
sc.exe query SessionManagerAgent
```

Atualizar:

```powershell
.\deploy\agent\windows\update-agent.ps1 -PublishPath .\publish\agent-win-x64
```

Desinstalar:

```powershell
.\deploy\agent\windows\uninstall-agent.ps1 -ServiceName "SessionManagerAgent"
```

Mais detalhes:

- `docs/operacao-agent-local.md`

## AD via Agent (MVP inicial)

Pré-requisito no servidor do agent:

- módulo PowerShell `ActiveDirectory` disponível
- agent instalado com `SupportsAd = true`

Pelo frontend (admin):

- acesse `/active-directory`
- selecione um servidor com capability AD
- carregue as OUs pelo seletor (endpoint `GET /api/ad/servers/{serverId}/organizational-units`)
- use formularios de criar usuario/reset de senha
- acompanhe status pelo `CommandId`

Exemplo criar usuário AD:

```powershell
$api = "https://api.seu-dominio.com"
$login = Invoke-RestMethod -Uri "$api/api/auth/login" -Method Post -ContentType "application/json" -Body (@{
  username="admin"; password="SENHA_ADMIN"
} | ConvertTo-Json)
$auth = @{ Authorization = "Bearer $($login.accessToken)" }

$create = Invoke-RestMethod -Uri "$api/api/ad/servers/<serverId>/users" `
  -Method Post -Headers $auth -ContentType "application/json" `
  -Body (@{
    username = "jose.silva"
    displayName = "Jose Silva"
    password = "SenhaForte@123"
    userPrincipalName = "jose.silva@empresa.local"
    organizationalUnitPath = "OU=Usuarios,DC=empresa,DC=local"
    changePasswordAtLogon = $true
  } | ConvertTo-Json)
```

Acompanhar status:

```powershell
Invoke-RestMethod -Uri "$api/api/agent-commands/$($create.id)" -Headers $auth
```

## Troubleshooting: API nao conecta no Postgres

Sintomas comuns:

- container `sessionmanager-api` reiniciando em loop
- log com `Npgsql.NpgsqlException` ou timeout de conexao

Checklist:

1. validar se o container `postgres` esta `healthy`
2. validar `POSTGRES_DB`, `POSTGRES_USER` e `POSTGRES_PASSWORD`
3. se senha foi alterada apos criar volume, recriar volume:

```bash
docker compose --env-file .env.local -f docker-compose.local.yml down -v
docker compose --env-file .env.local -f docker-compose.local.yml up --build -d
```

No Dockploy, aplique o mesmo procedimento no stack de deploy (`docker-compose.dockploy.yml`) com cuidado para nao descartar dados de producao.

## Troubleshooting: `NetworkError when attempting to fetch resource` no login

Causa comum em WSL/local:

- frontend buildado com `SESSIONMANAGER_FRONT_API_BASE_URL` de deploy (ex: `https://api.seu-dominio.com`)
- API local em `http://localhost:5000`

Correcao:

```bash
docker compose --env-file .env.dockploy -f docker-compose.dockploy.yml down
docker compose --env-file .env.local -f docker-compose.local.yml down -v
docker compose --env-file .env.local -f docker-compose.local.yml up --build -d
```

Depois disso, o frontend local volta a apontar para `http://localhost:5000`.

## Troubleshooting: Bind for 0.0.0.0:8080 failed (porta ocupada)

Sintoma:

- erro ao subir `sessionmanager-front` com mensagem `port is already allocated`

Correcao no `.env.dockploy` ou `.env.local`:

```bash
SESSIONMANAGER_FRONT_PORT=18080
```

Opcional para API:

```bash
SESSIONMANAGER_API_PORT=15000
```

Subir novamente:

```bash
docker compose --env-file .env.dockploy -f docker-compose.dockploy.yml up --build -d
```
