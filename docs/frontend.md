# Frontend (React)

## Stack

- React
- TypeScript
- React Router
- Vite

## Estrutura principal

- `src/frontend/src/App.tsx`: definicao de rotas
- `src/frontend/src/context/AuthContext.tsx`: estado de autenticacao e login/logout
- `src/frontend/src/context/ToastContext.tsx`: notificacoes globais
- `src/frontend/src/api/client.ts`: cliente HTTP central
- `src/frontend/src/components/AppLayout.tsx`: layout lateral
- `src/frontend/src/pages/*`: paginas do sistema

## Rotas de tela

- `/login`
- `/dashboard`
- `/sessions`
- `/active-directory` (somente admin)
- `/agents`
- `/audit`
- `/settings` (somente admin)
- `/users` (somente admin)

## Controle de acesso no cliente

- `ProtectedRoute`: exige usuario autenticado
- `AdminRoute`: exige role `Administrator`
- em erro `401` de rotas autenticadas, cliente limpa sessao e redireciona automaticamente para `/login`
- tela de login exibe aviso de sessao expirada quando o redirecionamento automatico ocorre

Importante: o controle real de seguranca continua no backend.

## Pagina de sessoes

Comportamento atual:

- carrega servidores via `/api/servers`
- filtra apenas servidores com `supportsRds = true`
- seleciona servidor default
- carrega sessoes via `/api/sessions`
- atualizacao automatica a cada 30 segundos
- busca local por usuario, estado e ID
- acoes com confirmacao por modal:
  - desconectar
  - logoff (admin)
  - encerrar processo

## Dashboard

- consome `/api/dashboard/metrics`
- exibe:
  - sessoes ativas
  - sessoes desconectadas
  - acoes do dia
  - erros do dia

## Auditoria

- consome `/api/audit` com paginação
- filtro por termo de busca
- lista eventos e resultado de operacao

## Agentes

- consome `/api/servers`
- exibe status `Online/Offline/Sem agent` por servidor
- exibe capacidades por servidor (`RDS`, `AD`)
- mostra ultimo heartbeat e ultimo snapshot
- atualizacao automatica a cada 30 segundos

## Active Directory (admin)

- consome `/api/servers` filtrando `supportsAd = true`
- organiza operacoes em abas descritivas: `Buscar e bloquear usuários`, `Criar novo usuário`, `Redefinir senha de acesso`, `Acompanhar comandos`
- busca usuarios AD via `POST /api/ad/servers/{serverId}/users/search`
- lista status de conta (ativo/bloqueado/travado) por usuário retornado
- enfileira criacao de usuario via `POST /api/ad/servers/{serverId}/users`
- enfileira reset de senha via `POST /api/ad/servers/{serverId}/users/{username}/reset-password`
- enfileira bloqueio via `POST /api/ad/servers/{serverId}/users/{username}/block`
- enfileira desbloqueio via `POST /api/ad/servers/{serverId}/users/{username}/unblock`
- acompanha status via `GET /api/agent-commands/{commandId}` com polling
- ao concluir comando AD com sucesso, a tela reaproveita a ultima busca e atualiza automaticamente a lista de usuarios (sem exigir nova busca manual)

## Configuracoes (admin)

- consome `/api/settings`
- organiza interface em abas: `Parâmetros` e `Processos permitidos`
- permite atualizar cada chave via `PUT /api/settings/{key}`
- lista e gerencia processos permitidos

## Usuarios e perfis (admin)

- organiza interface em abas: `Novo usuário` e `Gerenciar usuários`
- lista usuarios
- cria novo usuario
- ativa/desativa usuario
- altera perfis por checkbox

## Integracao com API

- token JWT enviado em `Authorization`
- erros da API sao convertidos em `toast`
- URL base configuravel por `VITE_API_BASE_URL`

## UX e animacoes

- transicao suave entre rotas no `AppLayout` para reduzir troca brusca de contexto
- entrada progressiva de paineis e cards principais
- microinteracoes em botoes, inputs, tabelas e toasts (hover/focus/active)
- destaque visual refinado de status pills (`Online/Offline/Unknown`)
- animacao de abertura para modal e overlay
- suporte a acessibilidade com `prefers-reduced-motion` para reduzir animacoes

Arquivo exemplo:

- `src/frontend/.env.example`

## Build e deploy

Desenvolvimento:

```powershell
cd src/frontend
npm install
npm run dev
```

Build de producao:

```powershell
cd src/frontend
npm run build
```

Output:

- `src/frontend/dist`

Se `dist` existir, o backend serve os arquivos estaticos automaticamente.
