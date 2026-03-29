# Melhorias, Riscos e Proximas Iteracoes

## Backlog recomendado (priorizado)

## Prioridade alta

1. endurecimento de seguranca de producao
- obrigar chave JWT forte via secret manager
- politica de senha mais forte para criacao de usuarios
- lockout apos tentativas de login invalidas
- auditar eventos de login com sucesso/falha

2. observabilidade
- logs estruturados por correlacao de requisicao
- metricas de latencia por endpoint e por comando RDS
- health checks de banco e servico de sessoes

3. testes automatizados
- testes de servicos da camada `Application`
- testes de integracao de API com Postgres (container de teste)
- testes de parse de `query user` com amostras reais

## Prioridade media

4. suporte completo multi-servidor RDS
- cadastro/ativacao de varios servidores
- selecao por grupo/logica operacional
- agregacao de sessoes entre servidores

5. melhorias de UX operacional
- filtros server-side de sessao
- ordenacao por colunas
- exportacao de auditoria (CSV)

6. governanca de configuracao
- endpoint para selecionar servidor default ativo
- versionamento simples de configuracoes criticas

## Prioridade baixa

7. integracao corporativa
- Active Directory / SSO
- politicas de acesso por grupo AD

8. notificacoes
- alertas de erro administrativo recorrente
- notificacao de acoes sensiveis

## Riscos tecnicos atuais

1. variacao da saida do `query user`
- o parser atual cobre regex + fallback, mas pode exigir ajuste em ambientes/locale especificos

2. dependencia de permissao do host
- sem permissao correta da conta do processo, comandos administrativos falham

3. escala de auditoria
- sem politicas de limpeza/arquivo, tabela de auditoria pode crescer com o tempo

4. autenticao local apenas
- nao ha SSO/AD nesta versao inicial

## Divida tecnica conhecida

- falta suite de testes automatizados
- falta padrao de versionamento de API
- falta pipeline CI/CD

## Criterios para aceitar futuras melhorias

Cada melhoria deve:

1. manter separacao de camadas
2. manter seguranca de execucao via backend
3. manter rastreabilidade em auditoria
4. incluir documentacao atualizada em `docs/`
5. incluir validacao manual minima ou teste automatizado
