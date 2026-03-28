# Gerenciador de Sessões RDS

Aplicação web interna para operar sessões de usuários em Windows Server com RDS/RemoteApp, com foco em segurança, auditoria e simplicidade para usuários operacionais.

## Visão Geral

Esta primeira versão entrega:

- autenticação com JWT
- controle de acesso por perfis (`Administrator` e `Operator`)
- painel de sessões com ações de desconectar, logoff e encerramento de processo
- auditoria completa das ações administrativas
- configurações do sistema
- gestão de usuários/perfis
- lista de processos permitidos para `taskkill`

## Arquitetura

Arquitetura em camadas (Clean-ish):

- `WebApi`: camada HTTP/REST, autenticação/autorização e composição
- `Application`: regras de negócio, contratos, DTOs e orquestração de casos de uso
- `Domain`: entidades e constantes de domínio
- `Infrastructure`: EF Core/SQLite, repositórios, seed, JWT, hash de senha e integração com Windows
- `frontend` (React + Vite): interface web em português (Brasil)

Fluxo principal:

1. usuário autentica (`/api/auth/login`)
2. frontend envia token JWT nas chamadas
3. controllers aplicam autorização por perfil
4. serviços de aplicação executam regras e disparam integração Windows
5. ações sensíveis geram `AuditLog`

## Estrutura de Pastas

```text
.
├── SessionManager.slnx
├── src
│   ├── SessionManager.Domain
│   ├── SessionManager.Application
│   ├── SessionManager.Infrastructure
│   │   └── Data
│   │       └── Migrations
│   ├── SessionManager.WebApi
│   └── frontend
├── docs
└── README.md
```

## Decisões Técnicas

- **.NET 10 + ASP.NET Core** para backend principal
- **EF Core + SQLite** na V1, com abstrações que facilitam migração para PostgreSQL/SQL Server
- **JWT Bearer** para autenticação stateless
- **Password Hash PBKDF2** customizado com salt e comparação em tempo constante
- **Integração Windows encapsulada** em `WindowsSessionGateway` usando:
  - `query user`
  - `rwinsta`
  - `logoff`
  - `taskkill`
- **Sanitização e validação** de processo (whitelist + regex)

## Requisitos

- .NET SDK 10.x
- Node.js 22+
- Windows Server/Windows com comandos RDS disponíveis

## Como Rodar em Desenvolvimento

### 1. Restaurar pacotes

```powershell
dotnet restore src/SessionManager.WebApi/SessionManager.WebApi.csproj
cd src/frontend
npm install
```

### 2. Aplicar migration e subir backend

```powershell
dotnet tool run dotnet-ef database update `
  --project src/SessionManager.Infrastructure/SessionManager.Infrastructure.csproj `
  --startup-project src/SessionManager.WebApi/SessionManager.WebApi.csproj

dotnet run --project src/SessionManager.WebApi/SessionManager.WebApi.csproj
```

Backend por padrão em `http://localhost:5000` (conforme launch/profile local).

### 3. Rodar frontend

```powershell
cd src/frontend
npm run dev
```

Frontend em `http://localhost:5173` com proxy para `/api`.

## Como Publicar no Windows Server

### 1. Build frontend

```powershell
cd src/frontend
npm ci
npm run build
```

### 2. Publish backend

```powershell
dotnet publish src/SessionManager.WebApi/SessionManager.WebApi.csproj -c Release -o .\\publish
```

### 3. Execução

- rodar como serviço Windows (recomendado) ou IIS + ASP.NET Core Hosting Bundle
- garantir permissão de execução dos comandos administrativos (`query`, `rwinsta`, `logoff`, `taskkill`)
- configurar firewall/rede interna para acesso apenas local/interno

## Variáveis de Ambiente e Configuração

Arquivo base: `src/SessionManager.WebApi/appsettings.json`

Chaves principais:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SigningKey`
- `Jwt:ExpirationMinutes`
- `AdminSeed:Username`
- `AdminSeed:DisplayName`
- `AdminSeed:Password`
- `WindowsSession:CommandTimeoutSeconds`
- `Cors:AllowedOrigins`

## Configuração do SQLite

Connection string padrão:

```json
"Data Source=./data/sessionmanager.db"
```

O banco é criado automaticamente na inicialização via migration.

## Usuário Admin Inicial

Seed configurado por:

```json
"AdminSeed": {
  "Username": "admin",
  "DisplayName": "Administrador",
  "Password": "Admin@12345"
}
```

Importante: altere a senha padrão antes de produção.

## Segurança (Observações)

- nenhuma execução de comando parte do frontend
- validação de payload no backend e regex para processo
- kill de processo restrito à lista branca (`AllowedProcesses`)
- auditoria de ações com operador, servidor, sessão, processo, resultado e IP
- JWT com assinatura HMAC e claims de role

## Endpoints Principais

- `POST /api/auth/login`
- `GET /api/dashboard/metrics`
- `GET /api/sessions`
- `POST /api/sessions/disconnect`
- `POST /api/sessions/logoff` (admin)
- `POST /api/sessions/kill-process`
- `GET /api/audit`
- `GET/PUT /api/settings` (admin)
- `GET/POST/PATCH /api/users` (admin)
- `GET/POST/PATCH /api/allowed-processes` (admin)

## Migrations

Migration inicial criada em:

- `src/SessionManager.Infrastructure/Data/Migrations/20260327222913_InitialCreate.cs`

Comandos úteis:

```powershell
dotnet tool run dotnet-ef migrations add NomeMigration `
  --project src/SessionManager.Infrastructure/SessionManager.Infrastructure.csproj `
  --startup-project src/SessionManager.WebApi/SessionManager.WebApi.csproj

dotnet tool run dotnet-ef database update `
  --project src/SessionManager.Infrastructure/SessionManager.Infrastructure.csproj `
  --startup-project src/SessionManager.WebApi/SessionManager.WebApi.csproj
```

## Riscos Técnicos Conhecidos

- parse de `query user` pode variar com locale/versão do Windows e layout da saída
- operações administrativas dependem de permissões do usuário de execução da API
- ambiente com múltiplos servidores RDS ainda está em modelo inicial (entidade pronta, sem orquestração distribuída)

## Pontos de Atenção para Produção

- trocar `Jwt:SigningKey` por segredo forte e protegido
- remover credenciais padrão do `AdminSeed`
- configurar HTTPS interno (IIS/reverse proxy)
- restringir CORS para domínios internos reais
- habilitar rotação/retention de logs e rotina de backup do SQLite

## Próximos passos recomendados

1. adicionar integração com Active Directory/SSO corporativo
2. implementar paginação/filtros server-side nas sessões em tempo real
3. adicionar monitoramento (health checks + métricas Prometheus/OpenTelemetry)
4. criar testes automatizados de aplicação e integração
5. adicionar suporte completo multi-servidor RDS com seleção e agregação distribuída

## Documentacao detalhada

Para contexto tecnico completo e continuidade de evolucao, use:

- `docs/INDEX.md` (indice geral)
- `docs/codex.md` (contexto rapido do estado atual)
- `docs/arquitetura.md`
- `docs/api.md`
- `docs/banco-e-migrations.md`
- `docs/frontend.md`
- `docs/operacao-rds.md`
- `docs/runbook.md`
- `docs/melhorias.md`

## Executar como servico Windows (sem login de usuario)

Com a API publicada em `C:\Apps\SessionManager\publish`, instale o backend como servico:

```powershell
sc.exe create SessionManagerWebApi `
  binPath= "\"C:\Apps\SessionManager\publish\SessionManager.WebApi.exe\" --urls http://0.0.0.0:5000" `
  start= auto `
  obj= "LocalSystem"
```

Defina reinicio automatico em falhas e inicie:

```powershell
sc.exe failure SessionManagerWebApi reset= 86400 actions= restart/5000/restart/5000/restart/5000
sc.exe start SessionManagerWebApi
```

Comandos uteis de operacao:

```powershell
sc.exe query SessionManagerWebApi
sc.exe stop SessionManagerWebApi
sc.exe config SessionManagerWebApi start= demand
```

Observacoes importantes:

- use conta de servico com privilegios para `query user`, `rwinsta`, `logoff` e `taskkill` (ou `LocalSystem` em ambiente interno controlado)
- garanta permissao de escrita na pasta `data` ao usuario do servico
- se mudar `appsettings*.json`, reinicie o servico
- se aparecer `StartService FAILED 1053` com `SQLite Error 14`, ajuste `ConnectionStrings:DefaultConnection` para caminho absoluto (ex.: `Data Source=C:\\Apps\\SessionManager\\publish\\data\\sessionmanager.db`) e recrie/inicie o servico
- para acesso do frontend a partir de outras maquinas quando API e UI estao no mesmo host, use `VITE_API_BASE_URL=` (vazio) no build do frontend

## Deploy Dockploy (API e Front Separados)

Arquivos de deploy:

- `Dockerfile` (API only)
- `Dockerfile.front` (frontend estatico via Nginx)
- `docker-compose.dockploy.yml` (exemplo local com os dois servicos)
- `docs/dockploy.md` (passo a passo completo)

Modelo recomendado:

- API em `api.seu-dominio.com`
- Front em `app.seu-dominio.com`
- Agent no RDS de cada cliente (proxima fase)
