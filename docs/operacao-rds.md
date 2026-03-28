# Integracao Windows / RDS

## Objetivo

Encapsular operacoes administrativas de sessao RDS de forma segura no backend, sem execucao direta pelo frontend.

Base funcional original validada:

- `docs/Gerenciar-Sessoes-RDS.ps1`

## Componentes envolvidos

- `WindowsSessionGateway`
- `WindowsCommandExecutor`
- `SessionService`

## Comandos utilizados

- listar sessoes: `query user /server:{server}`
- desconectar sessao: `rwinsta {sessionId} /server:{server}`
- logoff: `logoff {sessionId} /server:{server}`
- encerrar processo: `taskkill /server {server} /FI "SESSION eq {sessionId}" /IM {processName} /F`

## Politica de seguranca aplicada

- nome de processo validado por regex
- extensao `.exe` normalizada no backend
- processo precisa existir e estar ativo em `AllowedProcesses`
- erros tecnicos sao sanitizados antes de retorno
- timeout de comando configuravel (`WindowsSession:CommandTimeoutSeconds`)

## Parse de `query user`

Estrategia de parse:

1. regex principal para formato padrao
2. fallback com split por espacos multiplos

Isso reduz risco de quebra por pequenas variacoes da saida.

## Auditoria de operacoes

Toda acao passa por `SessionService` e gera `AuditLog` com:

- operador autenticado
- acao (`DISCONNECT`, `LOGOFF`, `TASKKILL`, etc)
- servidor
- sessao
- usuario alvo (quando informado)
- processo (quando aplicavel)
- sucesso/erro
- mensagem de erro (quando houver)
- IP do cliente

## Dependencias de permissao no servidor

Para funcionar em producao, o processo da API precisa:

- executar comandos administrativos de sessao
- ter permissao no host RDS alvo

Se rodar via IIS/servico, validar a identidade do processo.

## Falhas comuns e tratamento

- timeout de comando: retorno de falha controlada
- sessao inexistente: erro retornado pelo comando e encapsulado
- processo fora da whitelist: bloqueio antes do comando
- parse parcial da listagem: fallback tenta recuperar campos

## Evolucoes recomendadas

- coletar codigo de erro padrao por tipo de comando
- telemetria de latencia por comando
- health check ativo de conectividade com servidores RDS
- suporte a operacao paralela em multiplos servidores
