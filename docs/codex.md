# Contexto Operacional (Codex)

Ultima atualizacao: 2026-03-28

## Objetivo do sistema

Aplicacao web interna para operacao de sessoes RDS/RemoteApp em Windows Server, com:

- autenticacao e autorizacao por perfil
- operacao segura de sessoes (listar, desconectar, logoff, encerrar processo)
- auditoria completa das acoes
- interface simples para usuario operacional

## Estado atual da aplicacao

Primeira versao funcional entregue e validada com:

- backend ASP.NET Core .NET 10 em arquitetura por camadas
- frontend React + Vite com telas minimas obrigatorias
- banco Postgres em container (local e stack de deploy Dockploy), com migrations e seed automatico na subida da API
- seed automatico de perfis, servidor padrao, configuracoes, processos permitidos e usuario admin
- integracao com comandos nativos do Windows (`query user`, `rwinsta`, `logoff`, `taskkill`)
- API do Agent Windows (MVP) com heartbeat, fila de comando por servidor, retorno de resultado e auditoria de execucao

## Stack tecnica

- Backend: ASP.NET Core (.NET 10)
- Frontend: React + TypeScript + Vite
- Banco: Postgres (stacks Docker local + Dockploy)
- Fallback local sem Docker: SQLite via `appsettings*.json`
- ORM: Entity Framework Core 10
- Auth: JWT Bearer
- Password Hash: PBKDF2 (com salt)

## Perfis e permissoes

- `Administrator`
  - acesso total
  - logoff de sessao
  - gestao de usuarios
  - gestao de configuracoes
  - gestao de processos permitidos
  - auditoria completa

- `Operator`
  - login
  - listar sessoes
  - desconectar sessao
  - encerrar processo permitido
  - auditoria basica

## Principais rotas API

- `POST /api/auth/login`
- `GET /api/dashboard/metrics`
- `GET /api/sessions`
- `POST /api/sessions/disconnect`
- `POST /api/sessions/logoff` (admin)
- `POST /api/sessions/kill-process`
- `GET /api/audit`
- `GET /api/settings` (admin)
- `PUT /api/settings/{key}` (admin)
- `GET /api/users` (admin)
- `POST /api/users` (admin)
- `PATCH /api/users/{id}/status` (admin)
- `PATCH /api/users/{id}/roles` (admin)
- `GET /api/allowed-processes` (admin)
- `POST /api/allowed-processes` (admin)
- `PATCH /api/allowed-processes/{id}/status` (admin)
- `POST /api/agent/heartbeat` (agent key)
- `POST /api/agent/next-command` (agent key)
- `POST /api/agent/commands/{commandId}/result` (agent key)
- `POST /api/agent-commands/servers/{serverId}/commands` (admin)
- `GET /api/agent-commands/{commandId}` (admin)

## Frontend (paginas)

- `/login`
- `/dashboard`
- `/sessions`
- `/audit`
- `/settings` (admin)
- `/users` (admin)

## Configuracoes importantes

Arquivo: `src/SessionManager.WebApi/appsettings.json`

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SigningKey`
- `Jwt:ExpirationMinutes`
- `AdminSeed:*`
- `WindowsSession:CommandTimeoutSeconds`
- `Agent:ApiKey`
- `Cors:AllowedOrigins`

## Seed inicial

- usuario: `admin`
- senha default: `Admin@12345`

Trocar em ambiente real.

## Regras de seguranca que NAO devem ser quebradas

- frontend nunca executa comando de sistema
- toda acao passa pelo backend
- processo para `taskkill` deve ser validado e estar na whitelist
- nao concatenar comando com input cru
- toda acao administrativa deve gerar auditoria

## Mapa da documentacao

Leia nesta ordem:

1. `docs/INDEX.md`
2. `docs/arquitetura.md`
3. `docs/backend.md`
4. `docs/api.md`
5. `docs/banco-e-migrations.md`
6. `docs/frontend.md`
7. `docs/operacao-rds.md`
8. `docs/melhorias.md`

## Historico de requisitos

O pedido original do projeto foi preservado em:

- `docs/codex-requisitos-iniciais.md`
