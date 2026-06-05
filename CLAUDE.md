# Project Manager - CLAUDE.md

## 📋 Visão Geral do Projeto

**Project Manager** é um sistema completo de gestão de projetos desenvolvido com:
- **Backend**: C# .NET 10 + Entity Framework Core + SQLite
- **Frontend**: Angular com componentes standalone
- **Autenticação**: JWT tokens com 120 minutos de expiração
- **API**: RESTful com CORS habilitado para localhost:4200

## 🏗️ Arquitetura

### Backend
- **Port**: 5000 (localhost:5000)
- **Framework**: ASP.NET Core Web API
- **Database**: SQLite (projectmanager.db)
- **ORM**: Entity Framework Core 10.0.8

#### Estrutura de Camadas
```
Controllers (API endpoints)
    ↓
Services (Lógica de negócio)
    ↓
Data/DbContext (Acesso aos dados)
    ↓
Models (Entidades do banco)
```

#### Controladores Principais
- **AuthController** (`/api/auth`)
  - POST `/login` - Autenticação com JWT
  - POST `/register` - Registro de novo usuário

- **ProjectsController** (`/api/projects`)
  - CRUD de projetos
  - Gerenciar membros de projeto
  - Listar projetos com membros e tarefas

- **TasksController** (`/api/projects/{projectId}/tasks`)
  - CRUD de tarefas de projeto
  - Atualizar status e progresso

### Frontend
- **Port**: 4200 (localhost:4200)
- **Framework**: Angular (standalone components)
- **Build**: dist/ProjectManagerWebUI

#### Estrutura de Componentes
```
app/
├── components/
│   ├── login/              - Autenticação
│   ├── dashboard/          - Visão geral dos projetos
│   ├── project-detail/     - Detalhes do projeto
│   └── tasks/              - Gerenciamento de tarefas
├── services/
│   ├── auth.service.ts     - Gerenciar autenticação
│   ├── project.service.ts  - API de projetos
│   ├── task.service.ts     - API de tarefas
│   └── auth.interceptor.ts - Adicionar JWT ao header
├── models/
│   ├── user.model.ts
│   ├── project.model.ts
│   ├── task.model.ts
│   └── project-member.model.ts
└── guards/
    └── auth.guard.ts       - Proteger rotas autenticadas
```

## 📊 Modelos de Dados

### User
```
- Id (int, PK)
- Email (string, UNIQUE)
- FullName (string)
- PasswordHash (string)
- Department (string?)
- Role (string?)
- IsActive (bool)
- CreatedAt (datetime)
- LastLoginAt (datetime?)
```

### Project
```
- Id (int, PK)
- Name (string)
- Description (string?)
- StartDate (datetime)
- EndDate (datetime?)
- Status (string) [Planning, Active, Completed, On Hold]
- Budget (decimal?)
- SpentAmount (decimal?)
- Priority (int) [1-5]
- Manager (string)
- CreatedAt (datetime)
- UpdatedAt (datetime)
```

### ProjectMember
```
- Id (int, PK)
- ProjectId (int, FK)
- UserId (int, FK)
- Role (string) [Member, Lead, Manager]
- JoinedAt (datetime)
- IsActive (bool)
```

### ProjectTask
```
- Id (int, PK)
- ProjectId (int, FK)
- Title (string)
- Description (string?)
- Status (string) [Pending, In Progress, Completed, Blocked]
- Priority (string) [Low, Medium, High, Urgent]
- DueDate (datetime)
- AssignedTo (string?)
- EstimatedHours (int?)
- ActualHours (int?)
- Progress (decimal?) [0-100]
- CreatedAt (datetime)
- UpdatedAt (datetime)
```

### ProjectStatusHistory
```
- Id (int, PK)
- ProjectId (int, FK)
- FromStatus (string)
- ToStatus (string)
- Reason (string?)
- ChangedBy (string?)
- ChangedAt (datetime)
```

## 🔐 Autenticação e Segurança

### JWT Configuration
```json
{
  "Key": "ProjectManagerWebAPI_SuperSecretKey_2026",
  "Issuer": "ProjectManagerWebAPI",
  "Audience": "ProjectManagerWebUI",
  "ExpiryMinutes": "120"
}
```

### CORS
- Origem permitida: `http://localhost:4200`
- Métodos: GET, POST, PUT, DELETE, OPTIONS
- Headers: Accept, Content-Type, Authorization

### Hash de Senhas
- Algoritmo: SHA256
- Formato: Base64

## 🚀 Como Executar

### Desenvolvimento Rápido
```bash
./start.sh          # macOS/Linux
start.bat           # Windows
```

### Execução Manual
```bash
# Terminal 1 - Backend
cd ProjectManager/ProjectManagerWebAPI
dotnet run

# Terminal 2 - Frontend
cd ProjectManager/ProjectManager/ProjectManagerWebUI
ng serve
```

### Build para Produção
```bash
# Backend
cd ProjectManager/ProjectManagerWebAPI
dotnet publish -c Release

# Frontend
cd ProjectManager/ProjectManager/ProjectManagerWebUI
ng build --configuration production
```

## 📝 Padrões de Código

### Backend
- **Namespace**: PascalCase (ex: `ProjectManagerWebAPI.Services`)
- **Classes**: PascalCase
- **Methods**: PascalCase
- **Variables**: camelCase
- **DTOs**: Sufixo "Dto" ou "Request"/"Response"

### Frontend
- **Components**: kebab-case em arquivos, PascalCase em classes
- **Services**: Sufixo ".service.ts"
- **Models**: Sufixo ".model.ts"
- **Variables**: camelCase
- **Constants**: UPPER_SNAKE_CASE

## 🔄 Fluxo de Dados Típico

```
Frontend (Angular Component)
    ↓
Frontend Service (HTTP Request)
    ↓
Backend Controller (Request validation)
    ↓
Backend Service (Business logic)
    ↓
EF Core DbContext (Database operation)
    ↓
SQLite Database
    ↓
EF Core (Query results)
    ↓
Backend Service (Map to DTO)
    ↓
Backend Controller (Return response)
    ↓
Frontend Service (Parse response)
    ↓
Frontend Component (Update UI)
```

## 🛠️ Principais Dependências

### Backend
- `Microsoft.EntityFrameworkCore` (v10.0.8)
- `Microsoft.EntityFrameworkCore.Sqlite` (v10.0.8)
- `Microsoft.AspNetCore.Authentication.JwtBearer` (v10.0.8)
- `System.IdentityModel.Tokens.Jwt` (v8.0.1)

### Frontend
- `@angular/core` (latest)
- `@angular/common` (latest)
- `@angular/router` (latest)
- `@angular/common/http` (latest)

## 📚 Arquivos Importantes

- `ProjectManagerWebAPI/appsettings.json` - Configuração do backend
- `ProjectManagerWebAPI/Program.cs` - Setup do projeto .NET
- `ProjectManagerWebAPI/Data/ApplicationDbContext.cs` - Contexto do banco
- `ProjectManager/ProjectManagerWebUI/src/app/app.config.ts` - Setup do Angular
- `ProjectManager/ProjectManagerWebUI/src/app/app.routes.ts` - Rotas do aplicativo
- `global.json` - Versão do .NET SDK

## 🔍 Debug e Desenvolvimento

### Backend
- Use `dotnet watch run` para recarregamento automático
- Use breakpoints no Visual Studio Code com a extensão C#
- Logs: Verifique a saída do console

### Frontend
- Use `ng serve --poll` em WSL ou máquinas virtuais
- Use as DevTools do Chrome (F12) para debug
- Use `ng build --stats-json` para análise de bundle

## 📈 Próximas Funcionalidades a Implementar

- [ ] Relatórios e dashboard com gráficos
- [ ] Notificações em tempo real (SignalR)
- [ ] Sistema de comentários em tarefas
- [ ] Upload de anexos/documentos
- [ ] Integração com calendário
- [ ] Filtros e busca avançada
- [ ] Paginação no frontend
- [ ] Testes unitários e E2E
- [ ] Tema escuro
- [ ] Responsividade mobile

## 🚨 Problemas Conhecidos e Soluções

### Porta já em uso
```bash
# Backend
lsof -i :5000  # Encontrar processo
kill -9 PID    # Matar processo

# Frontend
ng serve --port 4300  # Usar porta diferente
```

### CORS error
- Verifique se backend está em http://localhost:5000
- Verifique se frontend está em http://localhost:4200
- Limpe o localStorage se tiver token antigo

### Banco de dados travado
```bash
rm projectmanager.db
dotnet ef database update
```

## 📞 Comandos Úteis

```bash
# EF Core
dotnet ef migrations add <Name>    # Criar migração
dotnet ef database update          # Aplicar migrações
dotnet ef migrations remove        # Remover última migração

# Angular
ng generate component <name>       # Gerar componente
ng generate service <name>         # Gerar serviço
ng generate guard <name>           # Gerar guard
ng build --prod                    # Build otimizado
ng test                            # Rodar testes

# .NET
dotnet build                       # Compilar
dotnet run                         # Executar
dotnet publish -c Release          # Publicar
dotnet watch run                   # Recarregar automático
```

## 👨‍💻 Notas de Desenvolvimento

- O projeto foi criado com componentes standalone do Angular (nova sintaxe)
- O banco SQLite é criado automaticamente na primeira execução
- Senhas são hasheadas com SHA256 antes de armazenar
- O JWT inclui claims: sub (userId), email, name
- Implementar validação de entrada antes de aceitar dados

---

**Última atualização**: 2026-06-01
**Status**: ✅ Funcional e pronto para desenvolvimento
