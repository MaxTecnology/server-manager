# START HERE

Ultima atualizacao: 2026-03-29

## 1) Leitura inicial recomendada

1. `docs/STATUS-ATUAL.md`
2. `docs/arquitetura.md`
3. `docs/backend.md`
4. `docs/api.md`
5. `docs/DEPLOY-CHECKLIST.md`
6. `docs/dockploy.md`
7. `docs/operacao-agent-local.md`

## 2) Comandos rapidos (local WSL)

Opcional: copie `.env.local.example` para `.env.local` para personalizar credenciais locais.

Importante: para desenvolver localmente, use sempre `docker-compose.local.yml` + `.env.local` (nao use o compose de Dockploy no localhost).

```bash
docker compose --env-file .env.local -f docker-compose.local.yml up --build -d
docker compose --env-file .env.local -f docker-compose.local.yml ps
```

No ambiente local, o stack sobe com 3 containers:

- `postgres` (banco)
- `sessionmanager-api`
- `sessionmanager-front`

Saude da API:

```bash
curl http://localhost:${SESSIONMANAGER_API_PORT:-5000}/api/health
```

Frontend:

```bash
curl http://localhost:${SESSIONMANAGER_FRONT_PORT:-8080}/
```

## 3) MVP Agent Windows

Fluxo implementado:

1. agent envia heartbeat e registra/atualiza servidor
2. admin enfileira comando por servidor
3. agent faz poll de comando pendente
4. agent envia resultado
5. execucao fica auditada em `AuditLogs`

Operacao do agent (publish + servico Windows):

- `docs/operacao-agent-local.md`
- scripts em `deploy/agent/windows/`
