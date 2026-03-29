# Banco de Dados e Migrations

## Provider atual

- EF Core 10
- Postgres como padrao nas stacks Docker:
  - `docker-compose.local.yml` (WSL)
  - `docker-compose.dockploy.yml` (deploy)
- SQLite permanece como fallback para execucao sem Docker (connection string local em `appsettings*.json`)

Selecao de provider:

- `Host=` / `Server=` na connection string -> Npgsql/Postgres
- demais formatos -> SQLite

## Modelo de dados

Tabelas principais:

- `Users`
- `Roles`
- `UserRoles`
- `Servers`
- `AgentCommands`
- `AuditLogs`
- `Settings`
- `AllowedProcesses`

## Chaves e indices

## Users

- PK: `Id`
- unique: `Username`

## Roles

- PK: `Id`
- unique: `Name`

## UserRoles

- PK composta: `UserId + RoleId`
- FK para `Users` e `Roles`

## Servers

- PK: `Id`
- unique: `Name`
- unique: `Hostname`

## AgentCommands

- PK: `Id`
- FK: `ServerId`
- indice composto: `ServerId + Status + CreatedAtUtc`

## AuditLogs

- PK: `Id`
- indice em `CreatedAtUtc`

## Settings

- PK: `Id`
- unique: `Key`

## AllowedProcesses

- PK: `Id`
- unique: `ProcessName`

## Seed automatico

Executado em startup por `DatabaseInitializer`.

Inclui:

- perfis default: `Administrator`, `Operator`
- servidor default: `Environment.MachineName`
- settings:
  - `AutoRefreshSeconds = 30`
  - `DefaultServerName = {machineName}`
- processos permitidos default:
  - `excel.exe`
  - `winword.exe`
  - `chrome.exe`
- usuario admin inicial (configuravel em `AdminSeed`)

## Migrations

Arquivos:

- `src/SessionManager.Infrastructure/Data/Migrations/20260327222913_InitialCreate.cs`
- `src/SessionManager.Infrastructure/Data/Migrations/20260328224306_AddAgentWindowsMvp.cs`

Snapshot:

- `src/SessionManager.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs`

## Comandos uteis

## Criar migration

```powershell
dotnet tool run dotnet-ef migrations add NomeMigration `
  --project src/SessionManager.Infrastructure/SessionManager.Infrastructure.csproj `
  --startup-project src/SessionManager.WebApi/SessionManager.WebApi.csproj
```

## Aplicar migration

```powershell
dotnet tool run dotnet-ef database update `
  --project src/SessionManager.Infrastructure/SessionManager.Infrastructure.csproj `
  --startup-project src/SessionManager.WebApi/SessionManager.WebApi.csproj
```

## Reverter ultima migration (somente desenvolvimento)

```powershell
dotnet tool run dotnet-ef migrations remove `
  --project src/SessionManager.Infrastructure/SessionManager.Infrastructure.csproj `
  --startup-project src/SessionManager.WebApi/SessionManager.WebApi.csproj
```

## Operacao com Docker

WSL local:

```bash
docker compose --env-file .env.local -f docker-compose.local.yml up --build -d
```

Dockploy/deploy:

```bash
docker compose --env-file .env.dockploy -f docker-compose.dockploy.yml up --build -d
```

A API aplica migrations automaticamente na subida.

## Observacoes para evolucao de banco

- mudancas de provider devem ficar concentradas em `Infrastructure`
- camada `Application` nao deve depender de tipos especificos de provider
- antes de cutover em producao:
  - congelar schema
  - gerar script de migration
  - executar backup do banco
