# Tarefas AD e RDS por Servidor

Ultima atualizacao: 2026-03-29

## Objetivo

Organizar a evolucao do produto para operar servidores com perfis diferentes:

- somente RDS
- somente Active Directory
- RDS + AD

## Fase 1: Capability por servidor (MVP base)

- [x] adicionar `SupportsRds` e `SupportsAd` na entidade `Server`
- [x] migration para novas colunas no banco
- [x] sincronizar capabilities via heartbeat do agent
- [x] bloquear operacoes RDS quando `SupportsRds = false`
- [x] bloquear operacoes AD quando `SupportsAd = false`
- [x] expor capabilities em `GET /api/servers`
- [x] frontend de sessoes filtrar somente servidores com `supportsRds = true`
- [x] frontend de agentes mostrar badges de capability
- [x] criar tela admin `Active Directory` no frontend

## Fase 2: Operacao AD no frontend (expansao)

- [x] listar OUs AD por servidor (snapshot + seletor no frontend para criar usuario)
- [ ] listar usuarios AD por servidor
- [ ] buscar usuario AD por login/display name
- [ ] acao de desbloquear conta AD
- [ ] acao de forcar expiracao de senha
- [ ] feedback de validacao de senha por politica do dominio

## Fase 3: Seguranca e observabilidade

- [ ] chave de agent por servidor (nao global) com rotacao
- [ ] mascarar `commandText` sensivel em respostas admin
- [ ] dashboard de falhas por agent (ultimos erros)
- [ ] alerta de agent offline por janela de tempo
- [ ] retencao e limpeza de `AgentCommands` antigos

## Fase 4: Experiencia operacional

- [ ] cadastro/edicao manual de capability no painel (override admin)
- [ ] wizard de instalacao do agent por perfil (`RDS`, `AD`, `RDS+AD`)
- [ ] pagina de troubleshooting por servidor (status do servico, heartbeat, fila local)
- [ ] runbook de rollback do agent por versao

## Regras de execucao

- mudancas pequenas e incrementais
- sem refatoracao ampla sem aprovacao
- atualizar documentacao em `docs/` a cada mudanca de comportamento
