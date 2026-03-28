# Referencia da API

Base path: `/api`

Autenticacao:

- JWT Bearer no header `Authorization`
- formato: `Bearer {token}`

## Regras gerais

- rotas sem `[AllowAnonymous]` exigem usuario autenticado
- respostas de erro usam formato `{ "message": "..." }`
- perfis:
  - `Administrator`
  - `Operator`

## Autenticacao

## POST `/auth/login`

Permissao: publica

Request:

```json
{
  "username": "admin",
  "password": "Admin@12345"
}
```

Response 200:

```json
{
  "accessToken": "jwt...",
  "expiresAtUtc": "2026-03-27T23:00:00Z",
  "user": {
    "id": "guid",
    "username": "admin",
    "displayName": "Administrador",
    "roles": ["Administrator", "Operator"]
  }
}
```

## Saude

## GET `/health`

Permissao: publica

Response 200:

```json
{
  "status": "ok",
  "timestampUtc": "2026-03-27T22:00:00Z"
}
```

## Dashboard

## GET `/dashboard/metrics`

Permissao: autenticado

Response 200:

```json
{
  "activeSessions": 2,
  "disconnectedSessions": 1,
  "actionsToday": 10,
  "errorsToday": 1,
  "generatedAtUtc": "2026-03-27T22:00:00Z"
}
```

## Sessoes

## GET `/sessions?serverName={hostname}`

Permissao: `Administrator` ou `Operator`

Response 200:

```json
[
  {
    "sessionId": 3,
    "username": "usuario",
    "sessionName": "rdp-tcp#5",
    "state": "Active",
    "idleTime": "none",
    "logonTime": "3/27/2026 10:00 AM",
    "serverName": "SRV-RDS"
  }
]
```

## POST `/sessions/disconnect`

Permissao: `Administrator` ou `Operator`

Request:

```json
{
  "sessionId": 3,
  "serverName": "SRV-RDS",
  "targetUsername": "usuario"
}
```

## POST `/sessions/logoff`

Permissao: `Administrator`

Request igual ao `disconnect`.

## POST `/sessions/kill-process`

Permissao: `Administrator` ou `Operator`

Request:

```json
{
  "sessionId": 3,
  "serverName": "SRV-RDS",
  "targetUsername": "usuario",
  "processName": "excel.exe"
}
```

Observacoes:

- processo passa por validacao regex
- processo precisa estar ativo na whitelist (`AllowedProcesses`)

## Auditoria

## GET `/audit?page=1&pageSize=20&search=&action=&success=`

Permissao: `Administrator` ou `Operator`

Response 200:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0
}
```

Regra especial:

- para `Operator`, o campo `errorMessage` e mascarado para `null`
- para `Administrator`, auditoria e completa

## Configuracoes

## GET `/settings`

Permissao: `Administrator`

## PUT `/settings/{key}`

Permissao: `Administrator`

Request:

```json
{
  "value": "30",
  "description": "Intervalo em segundos"
}
```

## Usuarios

## GET `/users`

Permissao: `Administrator`

## GET `/users/roles`

Permissao: `Administrator`

## POST `/users`

Permissao: `Administrator`

Request:

```json
{
  "username": "operador1",
  "displayName": "Operador 1",
  "password": "SenhaForte123!",
  "roles": ["Operator"]
}
```

## PATCH `/users/{id}/status`

Permissao: `Administrator`

Request:

```json
{
  "isActive": true
}
```

## PATCH `/users/{id}/roles`

Permissao: `Administrator`

Request:

```json
{
  "roles": ["Administrator", "Operator"]
}
```

## Processos permitidos

## GET `/allowed-processes`

Permissao: `Administrator`

## POST `/allowed-processes`

Permissao: `Administrator`

Request:

```json
{
  "processName": "chrome.exe"
}
```

## PATCH `/allowed-processes/{id}/status`

Permissao: `Administrator`

Request:

```json
{
  "isActive": false
}
```

## Servidores

## GET `/servers`

Permissao: `Administrator` ou `Operator`

Retorna lista cadastrada no banco com indicador de default.
