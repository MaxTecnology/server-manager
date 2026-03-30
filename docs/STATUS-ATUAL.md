# Status Atual

Ultima atualizacao: 2026-03-29

## Arquitetura atual

- Backend ASP.NET Core .NET 10 em camadas (`WebApi`, `Application`, `Domain`, `Infrastructure`)
- Frontend React + Vite servido em container Nginx
- Banco em Postgres para local e para stack de deploy (`docker-compose.dockploy.yml`)
- Integracao de sessoes RDS via Agent Windows (API recebe heartbeat + snapshot e expoe para frontend)
- Fila de comandos do Agent mantida para operacoes administrativas controladas

## O que ja esta pronto

- Auth JWT com roles `Administrator` e `Operator`
- Operacoes RDS (listar, desconectar, logoff, kill process com whitelist)
- Auditoria operacional e administrativa
- Deploy em stack de containers (`postgres`, API, front)
- Stack local completo em containers (`postgres` + API + front)
- Ambiente local WSL separado de deploy via `docker-compose.local.yml`
- Agent MVP:
  - registro/heartbeat por servidor
  - capabilities por servidor (`SupportsRds`, `SupportsAd`) sincronizadas pelo heartbeat
  - envio de snapshot de sessoes (`query user`) para API
  - dashboard e `/sessions` consumindo snapshot do agent em ambiente Linux/WSL
  - menu `Agentes` no frontend com status online/offline e ultimo heartbeat/snapshot
  - enfileiramento de comando por servidor
  - poll do proximo comando pelo agent
  - retorno de resultado da execucao
  - auditoria de envio e resultado de comando
  - worker Windows (`SessionManager.Agent.Windows`) com heartbeat/poll/execucao/retry local
  - scripts de instalacao como servico (`deploy/agent/windows`)
- AD MVP inicial:
  - agent envia snapshot de OUs AD (estrutura organizacional) para a API
  - endpoint admin para listar OUs por servidor AD com dados do snapshot
  - endpoint admin para criar usuario AD via agent
  - endpoint admin para reset de senha AD via agent
  - comando sensivel AD protegido (criptografado) em repouso no banco
  - tela admin `Active Directory` no frontend (criar/reset + acompanhamento de status + seletor de OU)
  - bloqueio backend/frontend por capability do servidor (RDS x AD)

## O que falta (proxima etapa apos MVP)

- rollout do agent em servidores Windows reais (piloto controlado)
- modelo de seguranca do agent por cliente (rotacao/segredo por tenant)
- telemetria de disponibilidade do agent (offline/timeout)
- politica de expiracao/retenção da fila de comandos
- detalhe por agente (fila local, retries, ultimo erro de execucao)
- fluxo AD expandido no frontend (consulta de usuarios AD, bloqueio/desbloqueio, grupos)
