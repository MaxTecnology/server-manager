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

Comportamento atual:

- agrega sessoes de servidores ativos com snapshot recente do agent
- nao depende de comandos RDS locais na API Linux/WSL

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

Comportamento atual:

- em API Linux/WSL: retorna dados do ultimo snapshot recebido do agent para o servidor
- em API Windows: pode usar gateway local de sessao
- se o agent estiver sem heartbeat/snapshot recente, retorna `400` com mensagem de erro

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

Retorna lista cadastrada no banco com status operacional do agent.

Campos `supportsRds` e `supportsAd` sao usados para habilitar/desabilitar operacoes por servidor no frontend e backend.

Response 200:

```json
[
  {
    "id": "guid",
    "name": "SRV-RDS-G2A",
    "hostname": "SRV-RDS-G2A",
    "isDefault": true,
    "isActive": true,
    "supportsRds": true,
    "supportsAd": false,
    "agentId": "SRV-RDS-G2A-agent",
    "agentVersion": "0.1.0",
    "agentLastHeartbeatUtc": "2026-03-29T20:10:00Z",
    "agentSessionSnapshotUtc": "2026-03-29T20:09:50Z",
    "isAgentOnline": true,
    "hasRecentSnapshot": true
  }
]
```

## Agent (MVP)

Autenticacao do agent:

- header obrigatorio `X-Agent-Key`
- chave configurada em `Agent:ApiKey`

## POST `/agent/heartbeat`

Permissao: publica (com `X-Agent-Key`)

Request:

```json
{
  "serverName": "WSL-RDS",
  "hostname": "WSL-RDS",
  "agentId": "agent-windows-01",
  "agentVersion": "0.1.0",
  "supportsRds": true,
  "supportsAd": false
}
```

Response 200:

```json
{
  "serverId": "guid",
  "serverName": "WSL-RDS",
  "hostname": "WSL-RDS",
  "receivedAtUtc": "2026-03-28T22:45:29Z"
}
```

## POST `/agent/session-snapshot`

Permissao: publica (com `X-Agent-Key`)

Request:

```json
{
  "serverName": "WSL-RDS",
  "hostname": "WSL-RDS",
  "agentId": "agent-windows-01",
  "agentVersion": "0.1.0",
  "supportsRds": true,
  "supportsAd": false,
  "capturedAtUtc": "2026-03-29T19:40:45Z",
  "sessionsOutput": "USERNAME ... (saida do query user)"
}
```

Response 200:

```json
{
  "serverId": "guid",
  "serverName": "WSL-RDS",
  "hostname": "WSL-RDS",
  "receivedAtUtc": "2026-03-29T19:40:48Z",
  "capturedAtUtc": "2026-03-29T19:40:45Z"
}
```

## POST `/agent/ad-ou-snapshot`

Permissao: publica (com `X-Agent-Key`)

Request:

```json
{
  "serverName": "SRV-AD-01",
  "hostname": "SRV-AD-01",
  "agentId": "srv-ad-01-agent",
  "agentVersion": "0.1.0",
  "supportsRds": false,
  "supportsAd": true,
  "capturedAtUtc": "2026-03-30T00:30:00Z",
  "organizationalUnitsOutput": "[{\"name\":\"Usuarios\",\"distinguishedName\":\"OU=Usuarios,DC=empresa,DC=local\",\"canonicalName\":\"empresa.local/Usuarios\"}]"
}
```

Response 200:

```json
{
  "serverId": "guid",
  "serverName": "SRV-AD-01",
  "hostname": "SRV-AD-01",
  "receivedAtUtc": "2026-03-30T00:30:02Z",
  "capturedAtUtc": "2026-03-30T00:30:00Z"
}
```

## POST `/agent/next-command`

Permissao: publica (com `X-Agent-Key`)

Observacao: endpoint opcional para fila de comandos administrativos.

Request:

```json
{
  "hostname": "WSL-RDS",
  "agentId": "agent-windows-01"
}
```

Response 200:

```json
{
  "commandId": "guid",
  "commandText": "query user",
  "requestedAtUtc": "2026-03-28T22:45:30Z"
}
```

Sem comando pendente: `204 No Content`.

## POST `/agent/commands/{commandId}/result`

Permissao: publica (com `X-Agent-Key`)

Request:

```json
{
  "success": true,
  "resultOutput": "saida do comando",
  "errorMessage": null
}
```

Response 200:

```json
{
  "message": "Resultado recebido."
}
```

## Agent Commands (Admin)

## POST `/agent-commands/servers/{serverId}/commands`

Permissao: `Administrator`

Request:

```json
{
  "commandText": "query user"
}
```

Response 200:

```json
{
  "id": "guid",
  "serverId": "guid",
  "serverName": "WSL-RDS",
  "hostname": "WSL-RDS",
  "requestedBy": "admin",
  "commandText": "query user",
  "status": "Pending",
  "requestedAtUtc": "2026-03-28T22:45:30Z",
  "pickedAtUtc": null,
  "completedAtUtc": null,
  "assignedAgentId": null,
  "resultOutput": null,
  "errorMessage": null
}
```

## GET `/agent-commands/{commandId}`

Permissao: `Administrator`

Retorna estado e resultado atual do comando.

## Active Directory (MVP inicial)

Pré-requisito:

- o Agent do servidor alvo precisa ter módulo `ActiveDirectory` disponível (RSAT/Domain Services tools)

Fluxo:

1. API enfileira comando protegido (criptografado em repouso no banco)
2. Agent decripta localmente e executa PowerShell AD
3. status/resultado fica em `AgentCommands`
4. API valida que o servidor alvo possui `supportsAd = true`

## GET `/ad/servers/{serverId}/organizational-units`

Permissao: `Administrator`

Retorna OUs do snapshot mais recente enviado pelo agent AD.

Response 200:

```json
[
  {
    "name": "Usuarios",
    "distinguishedName": "OU=Usuarios,DC=empresa,DC=local",
    "canonicalName": "empresa.local/Usuarios",
    "depth": 0
  },
  {
    "name": "RMA",
    "distinguishedName": "OU=RMA,OU=Usuarios,DC=empresa,DC=local",
    "canonicalName": "empresa.local/Usuarios/RMA",
    "depth": 1
  }
]
```

## POST `/ad/servers/{serverId}/users`

Permissao: `Administrator`

Request:

```json
{
  "username": "jose.silva",
  "displayName": "Jose Silva",
  "password": "SenhaForte@123",
  "userPrincipalName": "jose.silva@empresa.local",
  "organizationalUnitPath": "OU=Usuarios,DC=empresa,DC=local",
  "changePasswordAtLogon": true
}
```

Response 200:

- mesmo contrato de `AgentCommandDto` (id do comando para acompanhamento)

## POST `/ad/servers/{serverId}/users/{username}/reset-password`

Permissao: `Administrator`

Request:

```json
{
  "password": "NovaSenha@123",
  "changePasswordAtLogon": true,
  "enableAccount": true
}
```

Response 200:

- mesmo contrato de `AgentCommandDto`
