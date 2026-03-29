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

Persistencia:

- volume do Postgres em `/var/lib/postgresql/data`

## 3) Smoke test Agent MVP

1. `POST /api/agent/heartbeat` com `X-Agent-Key`
2. login admin (`POST /api/auth/login`)
3. enfileirar comando (`POST /api/agent-commands/servers/{serverId}/commands`)
4. poll (`POST /api/agent/next-command`)
5. resultado (`POST /api/agent/commands/{commandId}/result`)
6. consultar comando (`GET /api/agent-commands/{commandId}`)
7. verificar auditoria (`GET /api/audit?search=AGENT_COMMAND`)

## 4) Agent Windows (servico)

1. publicar binario:

```powershell
pwsh -File .\deploy\agent\windows\publish-agent.ps1 -Configuration Release -Runtime win-x64 -OutputPath .\publish\agent-win-x64
```

2. instalar no Windows Server (PowerShell admin):

```powershell
.\install-agent.ps1 -ApiBaseUrl "https://api.seu-dominio.com" -ApiKey "<AgentApiKey>" -ServerName "SRV-RDS-01" -AgentId "agent-srv-rds-01"
```

3. validar servico:

```powershell
Get-Service SessionManagerAgent
sc.exe query SessionManagerAgent
```

4. validar que heartbeat atualiza servidor e que comandos concluem com auditoria.

Referencia operacional detalhada:

- `docs/operacao-agent-local.md`
