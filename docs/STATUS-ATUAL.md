# Status Atual

Ultima atualizacao: 2026-03-29

## Arquitetura atual

- Backend ASP.NET Core .NET 10 em camadas (`WebApi`, `Application`, `Domain`, `Infrastructure`)
- Frontend React + Vite servido em container Nginx
- Banco em Postgres para local e para stack de deploy (`docker-compose.dockploy.yml`)
- Integracao de operacao RDS encapsulada em gateway backend
- Novo modulo Agent Windows MVP (heartbeat, fila de comandos e resultado)

## O que ja esta pronto

- Auth JWT com roles `Administrator` e `Operator`
- Operacoes RDS (listar, desconectar, logoff, kill process com whitelist)
- Auditoria operacional e administrativa
- Deploy em stack de containers (`postgres`, API, front)
- Stack local completo em containers (`postgres` + API + front)
- Ambiente local WSL separado de deploy via `docker-compose.local.yml`
- Agent MVP:
  - registro/heartbeat por servidor
  - enfileiramento de comando por servidor
  - poll do proximo comando pelo agent
  - retorno de resultado da execucao
  - auditoria de envio e resultado de comando
  - worker Windows (`SessionManager.Agent.Windows`) com heartbeat/poll/execucao/retry local
  - scripts de instalacao como servico (`deploy/agent/windows`)

## O que falta (proxima etapa apos MVP)

- rollout do agent em servidores Windows reais (piloto controlado)
- modelo de seguranca do agent por cliente (rotacao/segredo por tenant)
- telemetria de disponibilidade do agent (offline/timeout)
- retries/politica de reentrega e expiracao de comandos
- tela de operacao de comandos do agent no frontend
