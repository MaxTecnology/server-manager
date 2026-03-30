# Backend (ASP.NET Core)

## Projeto

- `src/SessionManager.WebApi`
- `src/SessionManager.Application`
- `src/SessionManager.Infrastructure`
- `src/SessionManager.Domain`

## Pipeline HTTP

Configurado em `Program.cs`:

1. logging console/debug
2. DI (`AddInfrastructure`)
3. controllers
4. CORS (`InternalFrontend`)
5. autenticacao JWT
6. autorizacao com fallback global
7. inicializacao de banco (`DatabaseInitializer`)
8. mapeamento de controllers
9. hosting de arquivos estaticos do frontend (se `dist` existir)

## Controllers

## Publicos

- `AuthController`
- `HealthController`
- `AgentController` (com header `X-Agent-Key`)

## Protegidos

- `DashboardController`
- `SessionsController`
- `AuditController`
- `ServersController`
- `AgentCommandsController` (admin)
- `ActiveDirectoryController` (admin)
- `SettingsController` (admin)
- `UsersController` (admin)
- `AllowedProcessesController` (admin)

## Servicos de aplicacao

Principais classes em `Application/Services`:

- `AuthService`
- `SessionService`
- `AuditService`
- `DashboardService`
- `SettingsService`
- `UserService`
- `AllowedProcessService`
- `ServerService`
- `AgentService`
- `ActiveDirectoryService`

## Persistencia

Implementacoes em `Infrastructure/Repositories`:

- `UserRepository`
- `RoleRepository`
- `AuditLogRepository`
- `SettingRepository`
- `AllowedProcessRepository`
- `ServerRepository`
- `AgentCommandRepository`

`AppDbContext` tambem implementa `IUnitOfWork`.

## Seguranca

- hash de senha: `Pbkdf2PasswordHasher`
- JWT: `JwtTokenService`
- tempo de expiracao via `Jwt:ExpirationMinutes`
- roles no token via `ClaimTypes.Role`

## Integracao Windows

Camada de comando:

- `WindowsCommandExecutor` (controle de timeout e captura de output/erro)
- `WindowsSessionGateway` (operacoes RDS)

Servicos nao chamam `Process` direto; usam interfaces para isolamento.

## Auditoria no backend

Acoes administrativas de sessao e administracao de entidades geram `AuditLog`.

Campos principais:

- operador
- acao
- servidor
- sessao
- usuario alvo
- processo
- sucesso/erro
- mensagem de erro
- IP cliente

No MVP de Agent:

- `AGENT_COMMAND_ENQUEUE` (quando admin envia comando)
- `AGENT_COMMAND_RESULT` (quando agent retorna execucao)

No MVP inicial de AD:

- operacoes de criar usuario/reset de senha sao enfileiradas via agent
- agent envia snapshot periodico de OUs AD para a API
- endpoint admin de leitura de OUs usa o snapshot recebido do agent
- payload de comando AD fica protegido (criptografado) no banco
- API bloqueia operacao AD quando servidor nao tem `SupportsAd`

No fluxo de sessoes (RDS):

- API bloqueia operacoes de sessao quando servidor nao tem `SupportsRds`

## Convencoes adotadas

- regras de negocio em `Application`
- DTOs para contratos de entrada/saida
- retorno padronizado com `Result` e `Result<T>`
- validacao defensiva antes de acao de infraestrutura

## Pontos sensiveis para futuras alteracoes

1. nao mover regra de permissao para frontend apenas
2. nao remover validacao de processo antes de `taskkill`
3. nao bypassar `SessionService` para executar comando diretamente
4. manter auditoria em qualquer nova acao administrativa
