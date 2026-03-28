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
4. listagem de sessoes funciona para servidor default
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
- o servico precisa permissão de escrita em `publish\data`

## Troubleshooting: erro 1053 no servico

Se no Event Viewer aparecer SQLite Error 14: unable to open database file, o processo do servico esta subindo com caminho relativo para o SQLite.

Ajuste ConnectionStrings:DefaultConnection para caminho absoluto, por exemplo:

`json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=C:\\Apps\\SessionManager\\publish\\data\\sessionmanager.db"
}
`

Depois reinicie o servico.
