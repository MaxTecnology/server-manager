# Deploy no Dockploy (API e Front Separados)

Este projeto foi preparado para subir em dois containers:

- `sessionmanager-api` (ASP.NET Core)
- `sessionmanager-front` (React estatico via Nginx)

## 1) Preparar repositorio na maquina com Docker

Copie o repositorio para a maquina que roda Docker/Dockploy.

Arquivos importantes:

- `Dockerfile` (API)
- `Dockerfile.front` (Frontend)
- `docker-compose.dockploy.yml` (exemplo local com os dois)
- `deploy/nginx.front.conf`

## 2) Deploy da API no Dockploy

Build Context:

- raiz do repositorio

Dockerfile:

- `Dockerfile`

Porta interna:

- `5000`

Variaveis (Environment):

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection=Data Source=/app/data/sessionmanager.db`
- `Jwt__Issuer=SessionManager`
- `Jwt__Audience=SessionManager.Internal`
- `Jwt__SigningKey=<chave forte 32+>`
- `Jwt__ExpirationMinutes=480`
- `AdminSeed__Username=admin`
- `AdminSeed__DisplayName=Administrador`
- `AdminSeed__Password=<senha inicial forte>`
- `Cors__AllowedOrigins__0=https://app.seu-dominio.com`

Volume persistente:

- monte `/app/data` em volume (para manter SQLite entre deploys)

## 3) Deploy do Front no Dockploy

Build Context:

- raiz do repositorio

Dockerfile:

- `Dockerfile.front`

Build Arg:

- `VITE_API_BASE_URL=https://api.seu-dominio.com`

Porta interna:

- `80`

## 4) Validacao

- `GET https://api.seu-dominio.com/api/health` deve retornar `ok`
- `GET https://app.seu-dominio.com/` deve abrir o login
- login no front deve chamar `https://api.seu-dominio.com/api/auth/login`

## 5) Proximo passo (Agent)

Com API e front separados, o proximo passo e colocar apenas um Agent Windows no RDS de cada cliente, conectado por saida para o control plane (API cloud).
