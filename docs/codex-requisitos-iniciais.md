Quero que você desenvolva uma aplicação web interna completa para gerenciamento de sessões de usuários em um Windows Server com Remote Desktop Services / RemoteApp.

Contexto do projeto
- A aplicação será desenvolvida e executada no próprio servidor RDS.
- O acesso será somente pela rede interna.
- O objetivo é permitir que usuários operacionais, com pouco conhecimento técnico, consigam visualizar sessões e executar ações administrativas básicas sem precisar abrir RDP no servidor e sem usar Gerenciador de Tarefas.
- Já validamos um script PowerShell funcional que consegue:
  - listar sessões
  - desconectar sessão
  - fazer logoff
  - encerrar processo específico dentro de uma sessão
- Agora quero transformar isso em uma aplicação web profissional, segura, simples e pronta para uso interno.

Stack obrigatória
- Backend: ASP.NET Core com .NET 10
- Frontend: React
- Banco de dados: SQLite
- ORM: Entity Framework Core
- API REST
- A aplicação deve rodar no Windows Server
- O frontend pode ser servido pela própria aplicação ASP.NET Core no deploy final

Diretrizes arquiteturais
- Quero uma arquitetura organizada e profissional
- Separação de camadas:
  - Web/API
  - Application
  - Domain
  - Infrastructure
- O projeto deve ficar preparado para no futuro trocar SQLite por PostgreSQL ou SQL Server com mínimo impacto
- O projeto deve ficar preparado para no futuro suportar múltiplos servidores RDS
- Não quero código improvisado nem acoplamento excessivo

Objetivo funcional principal
Criar um painel web interno para:
1. autenticação de usuários
2. visualização de sessões do Windows Server
3. desconectar sessão
4. fazer logoff da sessão
5. encerrar processo específico de uma sessão
6. registrar auditoria completa das ações

Requisitos funcionais
1. Tela de login
2. Dashboard inicial
3. Tela de sessões com listagem em tabela
4. Tela de auditoria/histórico
5. Tela de configurações
6. Tela de usuários e perfis
7. Campo de busca por:
   - nome do usuário
   - ID da sessão
   - estado da sessão
8. Atualização manual e automática da lista de sessões
9. Paginação e filtros
10. Modais de confirmação para ações sensíveis

Dados que devem aparecer na tela de sessões
- ID da sessão
- nome do usuário
- nome da sessão
- estado
- tempo ocioso
- horário de logon
- servidor

Ações por sessão
- visualizar detalhes
- desconectar sessão
- fazer logoff
- encerrar processo

Na ação de encerrar processo
- abrir modal
- permitir digitar o nome do processo
- exemplo: excel.exe, winword.exe, chrome.exe
- validar a entrada
- permitir lista branca de processos autorizados

Regras de permissão
Criar no mínimo 2 perfis:
1. Administrador
2. Operador

Permissões do Operador
- login
- visualizar sessões
- desconectar sessão
- encerrar processo permitido
- visualizar apenas auditoria básica

Permissões do Administrador
- todas as permissões do operador
- fazer logoff
- gerenciar usuários
- gerenciar configurações
- visualizar auditoria completa
- gerenciar lista de processos permitidos

Requisitos de segurança
- nenhuma execução de comando pode ser disparada diretamente do frontend
- toda ação deve passar pelo backend
- validar toda entrada do usuário
- bloquear command injection
- nunca concatenar comando bruto com input sem sanitização
- idealmente encapsular as operações de sistema em um serviço próprio
- registrar logs técnicos e logs de auditoria
- proteger rotas por autenticação e autorização
- armazenar senhas com hash seguro
- criar usuário admin inicial por seed/configuração inicial

Integração com Windows
A aplicação deve executar localmente no Windows Server e interagir com o sistema operacional para:
- consultar sessões
- desconectar sessão
- fazer logoff
- encerrar processo em determinada sessão

Você deve implementar essa integração de forma robusta e segura, escolhendo a melhor abordagem entre:
- PowerShell
- query user
- query session
- logoff
- rwinsta
- taskkill

Importante:
- encapsular isso em um serviço de infraestrutura, por exemplo:
  - SessionManagementService
  - WindowsCommandService
- padronizar retorno de sucesso/erro
- tratar timeouts
- registrar saída de erro com segurança
- não expor detalhes sensíveis para o usuário final

Banco de dados
Utilizar SQLite nesta primeira versão.

Criar modelagem inicial para:
- Users
- Roles
- UserRoles
- Servers
- AuditLogs
- Settings
- AllowedProcesses

Exigências para o banco
- usar Entity Framework Core
- criar migrations
- usar configuração por connection string
- deixar preparado para futura troca de provider

Auditoria
Toda ação administrativa deve gerar registro contendo:
- data e hora
- operador autenticado
- ação executada
- servidor
- sessão afetada
- usuário afetado
- processo afetado, quando houver
- resultado
- mensagem de erro, se houver
- IP do cliente, se possível

Dashboard
Criar indicadores no dashboard:
- total de sessões ativas
- total de sessões desconectadas
- total de ações no dia
- total de erros administrativos no dia

Configurações
Criar tela/configuração para:
- intervalo de atualização automática
- nome do servidor
- lista de processos permitidos
- parâmetros gerais do sistema

Interface
Quero interface:
- profissional
- corporativa
- limpa
- simples para usuários pouco técnicos
- desktop-first
- com navegação lateral
- tabela com filtros
- modais de confirmação
- toasts de sucesso/erro
- feedback claro para operações

Textos da interface
- usar português do Brasil na interface
- usar nomes técnicos em inglês no código

Critérios de implementação
- entregar código real, não só explicação
- começar propondo a arquitetura final
- depois criar estrutura de pastas
- depois implementar backend
- depois frontend
- depois banco/migrations
- depois integração com Windows
- depois README
- o sistema deve ficar funcional na primeira versão
- priorizar organização, legibilidade e manutenção

Fluxo esperado do sistema
1. usuário acessa login
2. autentica
3. entra no dashboard
4. acessa tela de sessões
5. localiza uma sessão
6. executa uma ação
7. confirma a ação
8. backend executa
9. sistema registra auditoria
10. interface atualiza a listagem e exibe retorno

Entrega esperada
Quero que você gere:
1. arquitetura proposta
2. estrutura de pastas do projeto
3. backend ASP.NET Core com camadas organizadas
4. autenticação e autorização
5. endpoints REST
6. frontend React com telas mínimas
7. integração com SQLite
8. migrations
9. seed inicial de usuário administrador
10. serviço de integração com Windows
11. componentes reutilizáveis no frontend
12. README profissional com:
   - visão geral
   - arquitetura
   - decisões técnicas
   - como rodar em desenvolvimento
   - como publicar no Windows Server
   - variáveis de ambiente
   - configuração do SQLite
   - configuração de usuário admin inicial
   - observações de segurança
13. seção final chamada:
   Próximos passos recomendados

Telas mínimas obrigatórias
- Login
- Dashboard
- Sessões
- Auditoria
- Configurações
- Usuários e Perfis

Importante para a execução do trabalho
- Não entregue só pseudocódigo
- Não pare na explicação
- Gere uma primeira versão funcional do sistema
- Faça escolhas técnicas consistentes
- Se precisar decidir algo não especificado, escolha a opção mais profissional e segura para ambiente interno Windows
- Use boas práticas de ASP.NET Core, React e EF Core
- Mantenha a solução pronta para evolução futura

Quero que você execute o trabalho em etapas nesta ordem:
1. Propor arquitetura
2. Gerar estrutura do projeto
3. Implementar backend base
4. Implementar autenticação/autorização
5. Implementar módulo de sessões
6. Implementar auditoria
7. Implementar frontend
8. Integrar frontend ao backend
9. Configurar SQLite e migrations
10. Gerar README completo

No final, entregue também:
- sugestões de melhorias futuras
- riscos técnicos conhecidos
- pontos de atenção para produção