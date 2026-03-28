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
- `/audit`
- `/settings` (somente admin)
- `/users` (somente admin)

## Controle de acesso no cliente

- `ProtectedRoute`: exige usuario autenticado
- `AdminRoute`: exige role `Administrator`

Importante: o controle real de seguranca continua no backend.

## Pagina de sessoes

Comportamento atual:

- carrega servidores via `/api/servers`
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

## Configuracoes (admin)

- consome `/api/settings`
- permite atualizar cada chave via `PUT /api/settings/{key}`
- lista e gerencia processos permitidos

## Usuarios e perfis (admin)

- lista usuarios
- cria novo usuario
- ativa/desativa usuario
- altera perfis por checkbox

## Integracao com API

- token JWT enviado em `Authorization`
- erros da API sao convertidos em `toast`
- URL base configuravel por `VITE_API_BASE_URL`

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
