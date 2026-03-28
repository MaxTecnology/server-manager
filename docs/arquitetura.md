# Arquitetura da Aplicacao

## Visao geral

O sistema segue separacao por camadas:

- `SessionManager.WebApi`
- `SessionManager.Application`
- `SessionManager.Domain`
- `SessionManager.Infrastructure`
- `src/frontend` (React)

Objetivo principal: manter regras de negocio fora da camada HTTP e manter infraestrutura substituivel (ex: SQLite para SQL Server/PostgreSQL no futuro).

## Responsabilidade por camada

## 1) WebApi

Responsavel por:

- expor endpoints REST
- aplicar autenticacao e autorizacao
- traduzir resultado de servicos em resposta HTTP
- inicializar banco na subida (`DatabaseInitializer`)
- servir `frontend/dist` quando existir

Nao deve conter regra de negocio pesada.

## 2) Application

Responsavel por:

- casos de uso
- validacoes de negocio
- contratos (`Interfaces`)
- DTOs
- orquestracao entre repositorios e gateways externos

Exemplo: `SessionService` valida session id, valida processo, checa whitelist, chama gateway Windows e registra auditoria.

## 3) Domain

Responsavel por:

- entidades centrais
- constantes de dominio

Entidades:

- `User`
- `Role`
- `UserRole`
- `Server`
- `AuditLog`
- `Setting`
- `AllowedProcess`

## 4) Infrastructure

Responsavel por:

- persistencia EF Core/SQLite (`AppDbContext`, repositorios)
- seguranca tecnica (`Pbkdf2PasswordHasher`, `JwtTokenService`)
- integracao com Windows (`WindowsSessionGateway`, `WindowsCommandExecutor`)
- seed e setup de banco (`DatabaseInitializer`)

## 5) Frontend

Responsavel por:

- autenticar usuario
- renderizar UI operacional
- consumir API com token JWT
- aplicar protecao de rotas no cliente

## Dependencias entre projetos

Fluxo de dependencia planejado:

- `WebApi -> Application + Infrastructure`
- `Infrastructure -> Application + Domain`
- `Application -> Domain`
- `Domain -> (sem dependencia interna do projeto)`

## Fluxo tecnico principal

1. Usuario faz login (`POST /api/auth/login`).
2. `AuthService` valida senha hash e gera JWT.
3. Frontend guarda token e envia `Authorization: Bearer`.
4. Controller protegido chama servico da camada `Application`.
5. Servico executa regra e usa repositorio/gateway.
6. Auditoria e persistencia sao gravadas no SQLite.
7. API retorna resposta para UI.

## Fluxo de inicializacao

Na subida do backend:

1. cria pasta `data` se nao existir
2. aplica migrations pendentes
3. garante seed de:
   - roles
   - servidor default
   - configuracoes basicas
   - processos permitidos
   - usuario admin inicial

## Decisoes tecnicas importantes

- JWT com claims de `role`
- hash de senha com PBKDF2
- fallback policy de autorizacao global (rota exige autenticacao por padrao)
- integracao RDS encapsulada em gateway unico
- parse de `query user` com regex + fallback por split

## Pontos de extensao previstos

- trocar provider EF sem quebrar camada `Application`
- suportar multiplos servidores RDS usando entidade `Server`
- incluir novos comandos/acoes via `IWindowsSessionGateway`
- adicionar novos perfis de acesso mantendo padrao `RoleNames`
