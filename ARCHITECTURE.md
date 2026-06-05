# 🏗️ Arquitetura do Project Manager

## Diagrama de Componentes

```
┌─────────────────────────────────────────────────────────────┐
│                      Browser (User)                          │
└──────────────────────────┬──────────────────────────────────┘
                           │
                    HTTP/HTTPS
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                   FRONTEND (Angular)                         │
│  ┌─────────────────────────────────────────────────────────┐│
│  │            Components                                    ││
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ││
│  │  │   Login      │  │  Dashboard   │  │Project Detail│  ││
│  │  └──────────────┘  └──────────────┘  └──────────────┘  ││
│  └───────────────────────────┬──────────────────────────────┘│
│                              │                               │
│  ┌───────────────────────────▼──────────────────────────────┐│
│  │         Services + HTTP Client                           ││
│  │  ┌─────────────┐ ┌──────────────┐ ┌─────────────────┐  ││
│  │  │AuthService  │ │ProjectService│ │TaskService     │  ││
│  │  └─────────────┘ └──────────────┘ └─────────────────┘  ││
│  │         │              │                   │             ││
│  │    ┌────▼──────────────▼───────────────────▼────┐        ││
│  │    │     HTTP Interceptor (Add JWT)             │        ││
│  │    └────────────────┬──────────────────────────┘        ││
│  └─────────────────────┼────────────────────────────────────┘│
└─────────────────────────┼─────────────────────────────────────┘
                          │
                    REST API (JSON)
                          │
┌─────────────────────────▼─────────────────────────────────────┐
│                   BACKEND (.NET 10)                           │
│  ┌───────────────────────────────────────────────────────────┐│
│  │           API Layer (Controllers)                         ││
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   ││
│  │  │AuthController│  │ProjectsContr.│  │TasksContr.   │   ││
│  │  └──────────────┘  └──────────────┘  └──────────────┘   ││
│  └─────────┬──────────────────────────────────┬──────────────┘│
│            │                                  │               │
│  ┌─────────▼──────────────────────────────────▼────────────┐ │
│  │       Business Logic Layer (Services)                   │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │ │
│  │  │AuthService   │  │ProjectService│  │TaskService   │  │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  │ │
│  └─────────┬──────────────────────────────────┬────────────┘ │
│            │                                  │               │
│  ┌─────────▼──────────────────────────────────▼────────────┐ │
│  │     Data Access Layer (DbContext + EF Core)             │ │
│  │    ApplicationDbContext                                  │ │
│  │  ├─ Users (DbSet)                                        │ │
│  │  ├─ Projects (DbSet)                                     │ │
│  │  ├─ ProjectMembers (DbSet)                               │ │
│  │  ├─ ProjectTasks (DbSet)                                 │ │
│  │  └─ ProjectStatusHistories (DbSet)                       │ │
│  └──────────────────────┬─────────────────────────────────┘ │
└─────────────────────────┼──────────────────────────────────┘
                          │
                      SQLite
                          │
┌─────────────────────────▼──────────────────────────────────────┐
│                  Database (SQLite)                            │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ Tables:                                                 │ │
│  │  • Users                                                │ │
│  │  • Projects                                             │ │
│  │  • ProjectMembers                                       │ │
│  │  • ProjectTasks                                         │ │
│  │  • ProjectStatusHistories                               │ │
│  │  • Migrations History (__EFMigrationsHistory)           │ │
│  └─────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

## Fluxo de Autenticação

```
┌─────────────┐
│   Usuario   │
└──────┬──────┘
       │
       │ 1. Insere Email + Senha
       ▼
┌──────────────────────┐
│  LoginComponent      │
│  (Frontend)          │
└──────┬───────────────┘
       │
       │ 2. POST /api/auth/login (LoginRequest)
       ▼
┌──────────────────────────┐
│  AuthController          │
│  (Backend)               │
└──────┬───────────────────┘
       │
       │ 3. Valida credenciais
       ▼
┌──────────────────────────┐
│  AuthService             │
│  • Verifica password     │
│  • Gera JWT Token        │
└──────┬───────────────────┘
       │
       │ 4. Retorna LoginResponse
       ▼
┌──────────────────────────┐
│  LoginComponent          │
│  • Armazena token        │
│  • Redireciona           │
└──────┬───────────────────┘
       │
       │ 5. Acesso ao Dashboard
       ▼
┌──────────────────────────┐
│  HTTP Interceptor        │
│  Adiciona: Authorization │
│  Bearer {token}          │
└──────┬───────────────────┘
       │
       │ 6. Requisição com JWT
       ▼
┌──────────────────────────┐
│  Backend API             │
│  [Authorize] atributo    │
│  Valida JWT              │
└──────────────────────────┘
```

## Fluxo de Criação de Projeto

```
1. User clica "New Project"
   └─> ProjectForm abre

2. User preenche formulário
   └─> Name, Description, StartDate, etc.

3. User clica "Create"
   └─> DashboardComponent.createProject()

4. ProjectService.create(CreateProjectRequest)
   └─> HTTP POST /api/projects

5. ProjectsController.CreateProject()
   └─> Valida dados
   └─> Chama ProjectService.CreateProject()

6. ProjectService (Backend)
   └─> Cria nova entidade Project
   └─> DbContext.Projects.Add()
   └─> SaveChanges()

7. Salva no SQLite
   └─> Database.Tables.Projects <- Nova linha

8. Retorna ProjectDto
   └─> HTTP 201 Created

9. Frontend recebe
   └─> Atualiza lista de projetos
   └─> UI mostra novo projeto

10. User vê projeto no Dashboard ✅
```

## Estrutura de Dados (Database)

```
┌─────────────────────┐
│      Users          │
├─────────────────────┤
│ ✓ Id (PK)           │
│ ✓ Email (UNIQUE)    │
│ ✓ FullName          │
│ ✓ PasswordHash      │
│ ✓ Department        │
│ ✓ Role              │
│ ✓ IsActive          │
│ ✓ CreatedAt         │
│ ✓ LastLoginAt       │
└──────────┬──────────┘
           │ (1:N)
           │ Has Many
           ▼
┌─────────────────────────┐
│   ProjectMembers        │
├─────────────────────────┤
│ ✓ Id (PK)               │
│ ✓ ProjectId (FK)        │
│ ✓ UserId (FK) ◄─────────┼─ References Users
│ ✓ Role                  │
│ ✓ JoinedAt              │
│ ✓ IsActive              │
└──────────────┬──────────┘
               │
         (N:1)│ Many:One
               ▼
        ┌──────────────────┐
        │   Projects       │
        ├──────────────────┤
        │ ✓ Id (PK)        │
        │ ✓ Name           │
        │ ✓ Description    │
        │ ✓ StartDate      │
        │ ✓ EndDate        │
        │ ✓ Status         │
        │ ✓ Budget         │
        │ ✓ SpentAmount    │
        │ ✓ Priority       │
        │ ✓ Manager        │
        │ ✓ CreatedAt      │
        │ ✓ UpdatedAt      │
        └────────┬─────────┘
                 │ (1:N)
        ┌────────┴──────────┐
        │                   │
        ▼                   ▼
┌──────────────────┐  ┌───────────────────────┐
│  ProjectTasks    │  │ProjectStatusHistories │
├──────────────────┤  ├───────────────────────┤
│✓ Id (PK)         │  │✓ Id (PK)              │
│✓ ProjectId (FK)  │  │✓ ProjectId (FK)       │
│✓ Title           │  │✓ FromStatus           │
│✓ Description     │  │✓ ToStatus             │
│✓ Status          │  │✓ Reason               │
│✓ Priority        │  │✓ ChangedBy            │
│✓ DueDate         │  │✓ ChangedAt            │
│✓ AssignedTo      │  └───────────────────────┘
│✓ EstimatedHours │
│✓ ActualHours    │
│✓ Progress        │
│✓ CreatedAt       │
│✓ UpdatedAt       │
└──────────────────┘
```

## Padrões de Design Utilizados

### 1. **MVC Pattern**
- Model: Classes de entidade (User, Project, etc.)
- View: Componentes Angular
- Controller: Controllers da API

### 2. **Repository Pattern** (Via EF Core)
- DbContext atua como repository
- DbSet<T> para acesso aos dados

### 3. **Dependency Injection**
- Backend: IServiceCollection
- Frontend: Angular's DI container

### 4. **Service Layer**
- Lógica de negócio separada
- Reutilização de código

### 5. **DTO Pattern**
- Separação entre Model e Client
- Validação de entrada

### 6. **Guard Pattern** (Roteamento)
- AuthGuard protege rotas
- Redireciona para login se não autenticado

## Fluxo de Autorização

```
┌──────────────────┐
│ Angular Guard    │ ← Can Activate?
└────────┬─────────┘
         │
         ├─ Não autenticado?
         │   └─> Redireciona para /login
         │
         └─ Autenticado?
             └─> Permite acesso ✓
                 └─> Backend valida JWT
                     └─ Token válido?
                        ├─ Sim → Acesso
                        └─ Não → Erro 401
```

## Camadas da Aplicação

```
┌─────────────────────────────────────────────┐
│         Presentation Layer                  │
│  (Components, Templates, Styling)           │
│  Angular Components (Standalone)            │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│         Application/Service Layer           │
│  (Business Logic, HTTP Communication)       │
│  Frontend Services, Backend Services        │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│         Domain/Model Layer                  │
│  (Entities, Value Objects, Interfaces)      │
│  User, Project, Task (Models)               │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│         Data Access Layer                   │
│  (Database Context, Repositories)           │
│  ApplicationDbContext, EF Core               │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│         Database Layer                      │
│  (SQLite)                                   │
└─────────────────────────────────────────────┘
```

---

**Diagrama criado em**: 2026-06-01
**Versão do Projeto**: 1.0.0
