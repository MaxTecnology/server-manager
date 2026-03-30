# Deploy Checklist

Ultima atualizacao: 2026-03-29

## 1) Local WSL (desenvolvimento)

Subir stack:

```bash
docker compose --env-file .env.local -f docker-compose.local.yml up --build -d
```

Validar:

```bash
docker compose --env-file .env.local -f docker-compose.local.yml ps
curl http://localhost:${SESSIONMANAGER_API_PORT:-5000}/api/health
curl http://localhost:${SESSIONMANAGER_FRONT_PORT:-8080}/
```

Esperado em `ps`:

- `postgres` healthy
- `sessionmanager-api` up
- `sessionmanager-front` up

## 2) Dockploy/Producao

Usar somente:

```bash
docker compose --env-file .env.dockploy -f docker-compose.dockploy.yml up --build -d
```

Antes de subir:

- criar `.env.dockploy` a partir de `.env.dockploy.example` (ou configurar variaveis no Dockploy)

Variaveis obrigatorias de producao:

- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `SESSIONMANAGER_JWT_SIGNING_KEY`
- `SESSIONMANAGER_ADMIN_PASSWORD`
- `SESSIONMANAGER_AGENT_API_KEY`
- `SESSIONMANAGER_CORS_ORIGIN`
- `SESSIONMANAGER_FRONT_API_BASE_URL`

Variaveis opcionais de porta:

- `SESSIONMANAGER_API_PORT` (default `5000`)
- `SESSIONMANAGER_FRONT_PORT` (default `8080`)
- `SESSIONMANAGER_POSTGRES_PORT` (default `5432`, bind local para tunnel)

Persistencia:

- volume do Postgres em `/var/lib/postgresql/data`

## 3) Smoke test Agent MVP

1. `POST /api/agent/heartbeat` com `X-Agent-Key`
2. `POST /api/agent/session-snapshot` com saida do `query user`
3. login admin (`POST /api/auth/login`)
4. validar `GET /api/sessions?serverName={hostname}`
5. validar `GET /api/dashboard/metrics`
6. opcional: validar fila de comandos (`/api/agent-commands`, `/api/agent/next-command`, `/api/agent/commands/{id}/result`)
7. verificar auditoria (`GET /api/audit?search=AGENT`)

## 4) Agent Windows (servico)

1. publicar binario:

```powershell
pwsh -File .\deploy\agent\windows\publish-agent.ps1 -Configuration Release -Runtime win-x64 -OutputPath .\publish\agent-win-x64
```

2. instalar no Windows Server (PowerShell admin):

```powershell
.\install-agent.ps1 -ApiBaseUrl "https://api.seu-dominio.com" -ApiKey "<AgentApiKey>" -ServerName "SRV-RDS-01" -AgentId "agent-srv-rds-01" -AdOuSnapshotIntervalSeconds 300 -SupportsRds $true -SupportsAd $false
```

3. validar servico:

```powershell
Get-Service SessionManagerAgent
sc.exe query SessionManagerAgent
```

4. validar que heartbeat + snapshots (sessao e OU AD quando habilitado) atualizam servidor e aparecem no frontend.

## 5) AD MVP inicial (opcional)

1. garantir que o servidor alvo do agent tenha módulo `ActiveDirectory` disponível
2. garantir que o agent foi instalado com `-SupportsAd $true`
3. validar retorno de OUs em `GET /api/ad/servers/{serverId}/organizational-units`
4. enfileirar criação de usuário em `POST /api/ad/servers/{serverId}/users`
5. enfileirar reset de senha em `POST /api/ad/servers/{serverId}/users/{username}/reset-password`
6. acompanhar execução em `GET /api/agent-commands/{commandId}`

Referencia operacional detalhada:

- `docs/operacao-agent-local.md`
