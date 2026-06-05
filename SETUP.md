# 🔧 Guia de Instalação e Configuração - Project Manager

## Pré-requisitos

Antes de começar, certifique-se de ter instalado:

### 1. .NET 10 SDK
- **macOS/Linux/Windows**: Faça download do [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Verificar instalação:
  ```bash
  dotnet --version
  ```
  Deve exibir: `10.0.x`

### 2. Node.js e npm
- Faça download do [Node.js LTS](https://nodejs.org/)
- Verificar instalação:
  ```bash
  node --version
  npm --version
  ```

### 3. Angular CLI
- Instalar globalmente:
  ```bash
  npm install -g @angular/cli
  ```
- Verificar instalação:
  ```bash
  ng version
  ```

## ⚙️ Configuração do Projeto

### Passo 1: Backend (.NET)

```bash
# Navegar para a pasta do backend
cd ProjectManager/ProjectManagerWebAPI

# Restaurar dependências NuGet
dotnet restore

# Compilar o projeto
dotnet build

# Criar/atualizar banco de dados
dotnet ef database update

# (Opcional) Verificar migrações
dotnet ef migrations list
```

**Resultado esperado:**
- Arquivo `projectmanager.db` criado na pasta raiz
- Mensagem: "Build succeeded" sem erros

### Passo 2: Frontend (Angular)

```bash
# Navegar para a pasta do frontend
cd ProjectManager/ProjectManager/ProjectManagerWebUI

# Instalar dependências npm
npm install

# (Opcional) Compilar para produção
npm run build
```

**Resultado esperado:**
- Pasta `node_modules` criada
- Pasta `dist` criada (se rodar build)

## 🚀 Executando o Projeto

### Opção 1: Scripts Automatizados

**No macOS/Linux:**
```bash
cd ProjectManager
./start.sh
```

**No Windows:**
```bash
cd ProjectManager
start.bat
```

### Opção 2: Execução Manual em Dois Terminais

**Terminal 1 - Backend:**
```bash
cd ProjectManager/ProjectManagerWebAPI
dotnet run
```

Você deverá ver:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7159
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**Terminal 2 - Frontend:**
```bash
cd ProjectManager/ProjectManager/ProjectManagerWebUI
ng serve --open
```

Você deverá ver:
```
✔ Browser application bundle generation complete. X.XXX seconds
Application bundle generation complete.
```

### ✅ Verificar se tudo está funcionando

- **Frontend:** Abra [http://localhost:4200](http://localhost:4200)
- **Backend API:** Acesse [http://localhost:5000/api/auth/login](http://localhost:5000/api/auth/login) (deve retornar erro 405, não 404)

## 🗄️ Gerenciar o Banco de Dados

### Visualizar banco de dados

Instale uma ferramenta SQLite:
- **DB Browser for SQLite**: https://sqlitebrowser.org/
- **Visual Studio Code Extension**: SQLite Explorer

### Resetar banco de dados

```bash
cd ProjectManager/ProjectManagerWebAPI

# Remove o banco atual
rm projectmanager.db

# Recria o banco com migrações
dotnet ef database update
```

### Adicionar dados de teste

```sql
-- Execute este SQL no DB Browser ou em uma ferramenta similar

INSERT INTO Users (Email, FullName, PasswordHash, Department, Role, IsActive, CreatedAt)
VALUES ('teste@example.com', 'Teste User', 'hash_da_senha', 'TI', 'Admin', 1, datetime('now'));
```

## 🔐 Primeira Execução - Criar Conta

1. Abra http://localhost:4200
2. Será redirecionado para `/login`
3. Clique em "Register" (ou crie uma conta manualmente no banco)
4. Preencha email e senha
5. Login com suas credenciais

## 📁 Estrutura de Pastas Criada

```
ProjectManager/
├── ProjectManagerWebAPI/
│   ├── bin/
│   ├── obj/
│   ├── Migrations/
│   ├── Controllers/
│   ├── Models/
│   ├── Services/
│   ├── Data/
│   ├── DTOs/
│   ├── ProjectManagerWebAPI.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   └── projectmanager.db (criado após primeira execução)
│
├── ProjectManager/ProjectManagerWebUI/
│   ├── src/
│   ├── dist/
│   ├── node_modules/
│   ├── angular.json
│   ├── package.json
│   ├── tsconfig.json
│   └── ...
│
├── README.md
├── SETUP.md (este arquivo)
├── start.sh (macOS/Linux)
├── start.bat (Windows)
├── global.json
└── ...
```

## 🐛 Troubleshooting

### "Angular CLI não encontrado"
```bash
npm install -g @angular/cli
```

### "Porta 5000 já está em uso"
```bash
# Mude a porta no launchSettings.json
```

### "Porta 4200 já está em uso"
```bash
ng serve --port 4300
```

### "Erro de CORS"
- Verifique se o backend está rodando em `http://localhost:5000`
- Verifique se o frontend está rodando em `http://localhost:4200`
- Reinicie ambos os serviços

### "Erro ao conectar ao banco de dados"
```bash
# Delete e recrie o banco
cd ProjectManager/ProjectManagerWebAPI
rm projectmanager.db
dotnet ef database update
```

### "Package.json não encontrado"
```bash
# Você está na pasta errada. Navegue até:
cd ProjectManager/ProjectManager/ProjectManagerWebUI
npm install
```

## 📝 Próximos Passos

1. **Criar usuário de teste** no sistema
2. **Explorar o dashboard** em http://localhost:4200
3. **Criar primeiro projeto**
4. **Adicionar membros ao projeto**
5. **Criar tarefas** e rastrear progresso

## 💡 Dicas de Desenvolvimento

### Desenvolvimento em tempo real (Frontend)
Angular detecta mudanças automaticamente com `ng serve`. Basta salvar o arquivo!

### Desenvolvimento em tempo real (Backend)
Use `dotnet watch run` para recompilação automática:
```bash
dotnet watch run
```

### Debugging
- **Frontend**: Use as DevTools do Chrome (F12)
- **Backend**: Use o debugger do Visual Studio ou VS Code

## 📚 Documentação Adicional

- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Angular Documentation](https://angular.io/docs)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [SQLite Documentation](https://www.sqlite.org/docs.html)

## ✨ Próximas Melhorias Sugeridas

- [ ] Adicionar testes unitários
- [ ] Implementar paginação
- [ ] Adicionar filtros avançados
- [ ] Integração com calendário
- [ ] Notificações em tempo real (SignalR)
- [ ] Relatórios e gráficos
- [ ] Upload de arquivos
- [ ] Sistema de comentários
- [ ] Histórico detalhado de ações
- [ ] Exportar dados (CSV, PDF)

---

**Sucesso! 🎉 Seu projeto está pronto para desenvolvimento!**
