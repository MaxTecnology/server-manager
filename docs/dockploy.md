# Deploy no Dockploy (Postgres + API + Front)

Este projeto foi preparado para subir em tres containers:

- `postgres` (banco de dados)
- `sessionmanager-api` (ASP.NET Core)
- `sessionmanager-front` (React estatico via Nginx)

## 1) Preparar repositorio na maquina com Docker

Copie o repositorio para a maquina que roda Docker/Dockploy.

Arquivos importantes:

- `Dockerfile` (API)
- `Dockerfile.front` (Frontend)
- `docker-compose.dockploy.yml` (stack completo para deploy)
- `.env.dockploy.example` (template de variaveis)
- `deploy/nginx.front.conf`

## 2) Configurar variaveis de ambiente

Crie um arquivo `.env.dockploy` na raiz (ou configure no painel do Dockploy) com base em `.env.dockploy.example`.

Variaveis obrigatorias:

Banco Postgres:

- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`

API:

- `SESSIONMANAGER_JWT_SIGNING_KEY`
- `SESSIONMANAGER_ADMIN_PASSWORD`
- `SESSIONMANAGER_AGENT_API_KEY`
- `SESSIONMANAGER_CORS_ORIGIN`

Frontend:

- `SESSIONMANAGER_FRONT_API_BASE_URL`

Portas de host (opcionais):

- `SESSIONMANAGER_API_PORT` (default `5000`)
- `SESSIONMANAGER_FRONT_PORT` (default `8080`)

## 3) Deploy do stack

```bash
docker compose --env-file .env.dockploy -f docker-compose.dockploy.yml up --build -d
```

## 4) Validacao

- `GET https://api.seu-dominio.com/api/health` deve retornar `ok`
- `GET https://app.seu-dominio.com/` deve abrir o login
- login no front deve chamar `https://api.seu-dominio.com/api/auth/login`
- `postgres` deve estar `healthy` antes da API iniciar (healthcheck no compose)

Volume persistente:

- volume do Postgres em `/var/lib/postgresql/data`

## 5) Observacoes de operacao

- alteracao de senha do Postgres apos volume criado exige recreacao do volume
- nao usar credenciais default em producao
- o compose de deploy foi endurecido para falhar cedo se variavel obrigatoria nao estiver configurada
- se `8080` estiver ocupada, ajuste `SESSIONMANAGER_FRONT_PORT` (ex: `18080`)
- se `5000` estiver ocupada, ajuste `SESSIONMANAGER_API_PORT` (ex: `15000`)

## 6) Agent Windows com API no Dockploy

- configurar `SESSIONMANAGER_AGENT_API_KEY` forte no deploy
- instalar o agent no Windows Server usando `deploy/agent/windows/install-agent.ps1`
- usar `-ApiBaseUrl` apontando para a URL publica da API (ex: `https://api.seu-dominio.com`)
- usar no script a mesma chave configurada em `SESSIONMANAGER_AGENT_API_KEY`
- validar no painel que heartbeat e snapshot de sessoes chegam (`/api/agent/heartbeat` e `/api/agent/session-snapshot`)

## 7) Observacao para desenvolvimento local (WSL)

Para ambiente local, use `docker-compose.local.yml`.

Esse arquivo local sobe Postgres + API + Front para desenvolvimento integrado.

Mantenha `docker-compose.dockploy.yml` apenas para deploy.
