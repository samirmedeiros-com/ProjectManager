# ⚡ Quick Start - Project Manager

Comece a usar em **5 minutos**!

## 1️⃣ Primeiro Clone/Navegação
```bash
cd ProjectManager
```

## 2️⃣ Instalar Dependências

### Backend
```bash
cd ProjectManagerWebAPI
dotnet restore
```

### Frontend
```bash
cd ../ProjectManager/ProjectManagerWebUI
npm install
```

## 3️⃣ Iniciar Tudo (Opção Fácil)

**macOS/Linux:**
```bash
cd ProjectManager
./start.sh
```

**Windows:**
```bash
cd ProjectManager
start.bat
```

## 4️⃣ Acessar a Aplicação

- 🌐 **Frontend**: http://localhost:4200
- 🔌 **Backend API**: http://localhost:5000

## 5️⃣ Testar Autenticação

1. Acesse http://localhost:4200
2. Você será redirecionado para `/login`
3. Preencha email e senha para criar uma conta
4. Faça login
5. Você verá o dashboard vazio
6. Crie seu primeiro projeto!

---

## 🎯 Fluxo Básico de Uso

```
Login/Register 
    ↓
Dashboard (Lista de Projetos)
    ↓
Criar Novo Projeto
    ↓
Adicionar Membros da Equipe
    ↓
Criar Tarefas
    ↓
Rastrear Progresso
```

---

## 📁 Estrutura Resumida

```
ProjectManager/
├── ProjectManagerWebAPI/       ← Backend (.NET)
├── ProjectManager/
│   └── ProjectManagerWebUI/    ← Frontend (Angular)
├── README.md                   ← Documentação completa
├── SETUP.md                    ← Guia de instalação
├── CLAUDE.md                   ← Documentação interna
└── start.sh / start.bat        ← Scripts de execução
```

---

## 🚀 Próximos Passos

1. **Explorar os componentes**
   - Crie vários projetos
   - Adicione membros
   - Crie tarefas
   - Atualize status

2. **Entender o código**
   - Analise `ProjectManagerWebAPI/Program.cs`
   - Analise `ProjectManagerWebUI/src/app/app.config.ts`
   - Explore os services

3. **Adicionar funcionalidades**
   - Novos componentes Angular
   - Novos endpoints da API
   - Novos modelos de dados

---

## 🆘 Problemas? Veja:

- ❌ **Porta em uso?** → Mude com `--port 4300` (Angular) ou em `launchSettings.json` (.NET)
- ❌ **Erro de CORS?** → Reinicie ambos os serviços
- ❌ **Banco corrompido?** → Delete `projectmanager.db` e reinicie
- ❌ **npm error?** → Delete `node_modules` e `package-lock.json`, rode `npm install`

---

## 📚 Recursos Rápidos

- [Ver README.md](./README.md) para documentação completa
- [Ver SETUP.md](./SETUP.md) para instalação detalhada
- [Ver CLAUDE.md](./CLAUDE.md) para documentação de código
- [Documentação .NET](https://learn.microsoft.com/dotnet)
- [Documentação Angular](https://angular.io/docs)

---

**Pronto? Inicie com `./start.sh` ou `start.bat` e comece a construir! 🚀**
