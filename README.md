# Project Manager - Sistema de Gestão de Projetos

Um sistema completo de gestão de projetos desenvolvido com **C# .NET 10** (backend) e **Angular** (frontend), com autenticação JWT e banco de dados SQLite.

## 🏗️ Arquitetura

```
ProjectManager/
├── ProjectManagerWebAPI/          # Backend C# .NET 10
│   ├── Controllers/               # Controladores da API
│   ├── Models/                    # Modelos de dados
│   ├── Services/                  # Serviços de negócio
│   ├── Data/                      # Contexto do banco de dados
│   ├── DTOs/                      # Data Transfer Objects
│   └── Migrations/                # Migrações do banco de dados
│
└── ProjectManager/ProjectManagerWebUI/  # Frontend Angular
    ├── src/app/
    │   ├── components/            # Componentes Angular
    │   ├── services/              # Serviços Angular
    │   ├── models/                # Modelos TypeScript
    │   └── guards/                # Guards de autenticação
    └── dist/                      # Build de produção
```

## 🚀 Requisitos

- **.NET 10 SDK** ou superior
- **Node.js 18+** e **npm**
- **Angular CLI** (instalado globalmente: `npm install -g @angular/cli`)

## 📦 Instalação

### 1. Backend (.NET)

```bash
cd ProjectManager/ProjectManagerWebAPI
dotnet restore
dotnet build
```

### 2. Frontend (Angular)

```bash
cd ProjectManager/ProjectManager/ProjectManagerWebUI
npm install
```

## 🏃 Execução

### Iniciar o Backend

```bash
cd ProjectManager/ProjectManagerWebAPI
dotnet run
```

O backend estará disponível em: **http://localhost:5000**

O banco de dados SQLite será criado automaticamente em `projectmanager.db`

### Iniciar o Frontend

```bash
cd ProjectManager/ProjectManager/ProjectManagerWebUI
ng serve
```

O frontend estará disponível em: **http://localhost:4200**

## 🔐 Autenticação

O sistema usa **JWT (JSON Web Tokens)** para autenticação.

### Credenciais Padrão

Para testar, primeiro você precisa fazer um registro na tela de login.

## 📋 Funcionalidades

### ✅ Gerenciamento de Projetos
- Criar, editar e deletar projetos
- Visualizar lista de todos os projetos
- Atribuir gerenciador de projeto
- Definir orçamento e datas de início/término
- Definir status e prioridade

### 👥 Gerenciamento de Membros
- Adicionar membros aos projetos
- Definir roles dos membros (Member, Lead, Manager)
- Remover membros do projeto
- Visualizar histórico de membros

### ✓ Gerenciamento de Tarefas
- Criar tarefas dentro de projetos
- Atribuir tarefas a membros
- Definir prioridade e status
- Rastrear progresso (0-100%)
- Estimar e registrar horas gastas
- Definir datas de vencimento

### 📊 Monitoramento
- Dashboard com visão geral dos projetos
- Status de projetos em tempo real
- Progresso das tarefas
- Histórico de mudanças de status

## 🗄️ Banco de Dados

O projeto usa **Entity Framework Core** com **SQLite**.

### Modelos
- **User** - Utilizadores do sistema
- **Project** - Projetos
- **ProjectMember** - Membros de projetos
- **ProjectTask** - Tarefas dos projetos
- **ProjectStatusHistory** - Histórico de mudanças de status

## 🔑 API Endpoints

### Autenticação
- `POST /api/auth/login` - Login
- `POST /api/auth/register` - Registro

### Projetos
- `GET /api/projects` - Listar todos os projetos
- `GET /api/projects/{id}` - Obter projeto específico
- `POST /api/projects` - Criar novo projeto
- `PUT /api/projects/{id}` - Atualizar projeto
- `DELETE /api/projects/{id}` - Deletar projeto

### Membros do Projeto
- `GET /api/projects/{id}/members` - Listar membros
- `POST /api/projects/{id}/members` - Adicionar membro
- `DELETE /api/projects/members/{memberId}` - Remover membro

### Tarefas
- `GET /api/projects/{projectId}/tasks` - Listar tarefas
- `GET /api/projects/{projectId}/tasks/{id}` - Obter tarefa
- `POST /api/projects/{projectId}/tasks` - Criar tarefa
- `PUT /api/projects/{projectId}/tasks/{id}` - Atualizar tarefa
- `DELETE /api/projects/{projectId}/tasks/{id}` - Deletar tarefa

## 🔒 Segurança

- Autenticação JWT com tokens que expiram em 120 minutos
- Hash de senhas com SHA256
- CORS configurado para localhost:4200
- Guards de autenticação nas rotas protegidas

## 📝 Configuração

### appsettings.json (Backend)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=projectmanager.db"
  },
  "JwtSettings": {
    "Key": "ProjectManagerWebAPI_SuperSecretKey_2026",
    "Issuer": "ProjectManagerWebAPI",
    "Audience": "ProjectManagerWebUI",
    "ExpiryMinutes": "120"
  }
}
```

## 🛠️ Desenvolvimento

### Criar Migração (Backend)

```bash
cd ProjectManager/ProjectManagerWebAPI
dotnet ef migrations add <NomeDaMigracao>
dotnet ef database update
```

### Componentes Angular

Para criar um novo componente:

```bash
ng generate component components/novo-componente
```

## 📄 Estrutura de Pastas

```
ProjectManager/
├── ProjectManagerWebAPI/
│   ├── bin/                   # Binários compilados
│   ├── obj/                   # Artefatos de build
│   ├── Controllers/           # Controladores API
│   ├── Models/                # Modelos do banco
│   ├── Services/              # Lógica de negócio
│   ├── Data/                  # Contexto EF Core
│   ├── DTOs/                  # Transfer objects
│   ├── Migrations/            # Migrações do banco
│   ├── appsettings.json       # Configuração
│   └── Program.cs             # Entry point
│
└── ProjectManager/ProjectManagerWebUI/
    ├── src/
    │   ├── app/
    │   │   ├── components/    # Componentes Angular
    │   │   ├── services/      # Serviços HTTP
    │   │   ├── models/        # Interfaces TypeScript
    │   │   ├── guards/        # Guards de rota
    │   │   └── app.config.ts  # Configuração
    │   ├── main.ts            # Entry point
    │   └── styles.css         # Estilos globais
    ├── dist/                  # Build de produção
    ├── angular.json           # Config do Angular
    ├── package.json           # Dependências npm
    └── tsconfig.json          # Config TypeScript
```

## 🐛 Troubleshooting

### Erro de conexão ao banco de dados
- Verifique se o arquivo `projectmanager.db` foi criado
- Limpe e recrie o banco: `dotnet ef database drop && dotnet ef database update`

### CORS errors
- Verifique se o backend está rodando em `http://localhost:5000`
- Verifique se o frontend está rodando em `http://localhost:4200`

### Erros de autenticação
- Verifique se o token JWT está sendo enviado nos headers
- Verifique se a chave JWT no `appsettings.json` é a mesma do frontend

## 📚 Recursos Adicionais

- [Documentação do .NET 10](https://learn.microsoft.com/pt-br/dotnet/)
- [Documentação do Angular](https://angular.io/docs)
- [Entity Framework Core](https://learn.microsoft.com/pt-br/ef/core/)
- [JWT.io](https://jwt.io/)

## 👨‍💻 Autor

Projeto criado com Claude Code

## 📄 Licença

Este projeto é fornecido como está para fins educacionais.
