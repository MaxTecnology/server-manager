# Banco de Dados e Migrations

## Provider atual

- SQLite
- EF Core 10

Connection string default:

```json
"Data Source=./data/sessionmanager.db"
```

Em desenvolvimento:

```json
"Data Source=./data/sessionmanager.dev.db"
```

## Modelo de dados

Tabelas principais:

- `Users`
- `Roles`
- `UserRoles`
- `Servers`
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

## Migration inicial

Arquivo:

- `src/SessionManager.Infrastructure/Data/Migrations/20260327222913_InitialCreate.cs`

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

## Observacoes para evolucao de banco

- troca de provider deve ficar concentrada em `Infrastructure`:
  - `DependencyInjection`
  - pacotes EF do provider alvo
  - connection string
- camada `Application` nao deve depender de tipos especificos de SQLite
- para migrar a producao com seguranca:
  - congelar schema
  - gerar script de migracao
  - executar backup antes de cutover
