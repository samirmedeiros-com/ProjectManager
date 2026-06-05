# Instruções de Deploy em Produção

## Pré-requisitos
- .NET 10.0 SDK instalado
- Acesso ao banco Oracle com credenciais do usuário `project`
- Connection string do Oracle configurada em `appsettings.json`

## Passos para Deploy

### 1. Preparar o Ambiente

```bash
cd ProjectManager/ProjectManagerWebAPI
dotnet restore
dotnet build -c Release
```

### 2. Aplicar Migrações do Banco de Dados

⚠️ **IMPORTANTE**: Este passo deve ser executado APENAS UMA VEZ antes de iniciar a aplicação

```bash
dotnet ef database update
```

Este comando:
- Criará todas as tabelas no Oracle
- Criará o usuário Admin padrão: **admin@admin.local** / **nsam150123**
- Não executará novamente nas próximas execuções da aplicação

### 3. Iniciar a Aplicação em Produção

```bash
ASPNETCORE_ENVIRONMENT=Production dotnet run -c Release
```

## Configuração

### Connection String Oracle

A connection string está configurada em `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=10.2.3.30)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=CRHPOTS01.databasesubnet.vcndpd.oraclevcn.com)));User Id=project;Password=dpd#2026;"
}
```

### Credenciais Admin Padrão

- **Email**: admin@admin.local
- **Password**: nsam150123

⚠️ **Altere a password após o primeiro login!**

## Verificação

Após o deploy:

1. Verifique se a aplicação está rodando na porta 5000
2. Acesse http://localhost:5000/api/auth/login (deve retornar 405 Method Not Allowed)
3. Faça login com as credenciais acima
4. Altere a password do usuário admin na interface

## Troubleshooting

### Erro: "ORA-00902: tipo de dados inválido"
- Certifique-se de que está usando Oracle 11g+ (suporta NUMBER(1) e DECIMAL)
- Verifique a connection string

### Erro: "The model for context 'ApplicationDbContext' has pending changes"
- Não execute `dotnet run` diretamente se houver mudanças no model
- Execute `dotnet ef migrations add NomeMigracao` primeiro
- Depois `dotnet ef database update`

### Erro de Conexão
- Verifique se o Oracle está acessível no IP/porta informado
- Verifique as credenciais do usuário `project`
- Teste a conexão com SQL\*Plus antes de fazer deploy

## Manutenção

### Adicionar Nova Migração (após mudanças no modelo)

```bash
dotnet ef migrations add NomedaMigracao
dotnet ef database update
```

### Rollback de Migração

```bash
dotnet ef migrations remove  # Remove a última migração não aplicada
dotnet ef database update [PreviousMigration]  # Volta para uma migração anterior
```

## Variáveis de Ambiente

```bash
# Ambiente de execução
ASPNETCORE_ENVIRONMENT=Production

# URL da aplicação
ASPNETCORE_URLS=http://0.0.0.0:5000

# Logging
Logging__LogLevel__Default=Information
```
