# 🐳 Docker Setup - Project Manager

## Overview

Este projeto inclui Dockerfiles otimizados para o backend (.NET) e frontend (Angular), além de um `docker-compose.yml` para orquestração completa.

## 📋 Pré-requisitos

- Docker Desktop instalado
- Docker Compose v2+

## 🚀 Início Rápido

### Executar tudo com Docker Compose (Recomendado)

```bash
# Na raiz do projeto
docker-compose up -d

# Verificar status dos serviços
docker-compose ps

# Ver logs em tempo real
docker-compose logs -f
```

Após iniciar:
- **Frontend**: http://localhost:4200
- **Backend API**: http://localhost:5000

### Parar os serviços

```bash
docker-compose down
```

## 🏗️ Build Manual

### Backend (.NET)

```bash
cd ProjectManagerWebAPI

# Build da imagem
docker build -t project-manager-backend .

# Executar container
docker run -d \
  -p 5000:5000 \
  -v $(pwd)/projectmanager.db:/app/projectmanager.db \
  --name project-manager-backend \
  project-manager-backend
```

### Frontend (Angular)

```bash
cd ProjectManager/ProjectManagerWebUI

# Build da imagem
docker build -t project-manager-frontend .

# Executar container
docker run -d \
  -p 4200:4200 \
  --name project-manager-frontend \
  project-manager-frontend
```

## 📝 Configuração

### Variáveis de Ambiente

**Backend (.NET)**
```dockerfile
ASPNETCORE_URLS=http://+:5000
ASPNETCORE_ENVIRONMENT=Production
```

**Frontend (Angular)**
```dockerfile
NODE_ENV=production
API_URL=http://backend:5000  # Usado no docker-compose
```

## 🗂️ Estrutura de Arquivos Docker

```
ProjectManager/
├── docker-compose.yml           # Orquestração dos serviços
├── ProjectManagerWebAPI/
│   ├── Dockerfile              # Multi-stage build para .NET
│   └── .dockerignore           # Arquivos a ignorar
├── ProjectManager/ProjectManagerWebUI/
│   ├── Dockerfile              # Multi-stage build para Node
│   └── .dockerignore           # Arquivos a ignorar
└── DOCKER.md                   # Este arquivo
```

## 🔍 Troubleshooting

### Porta já em uso
```bash
# Verificar qual processo está usando a porta
lsof -i :5000  # Backend
lsof -i :4200  # Frontend

# Usar portas diferentes no docker-compose.yml
# Ou matar o processo
kill -9 <PID>
```

### Banco de dados não persistindo
```bash
# Certifique-se de que o volume está configurado
# no docker-compose.yml:
volumes:
  - ./ProjectManagerWebAPI/projectmanager.db:/app/projectmanager.db
```

### Frontend não conecta ao backend
```bash
# Verificar se os serviços podem se comunicar
docker-compose logs frontend

# O backend deve estar acessível em: http://backend:5000
# Dentro da rede do docker-compose
```

## 🔐 Segurança

Para produção:
1. Use variáveis de ambiente secretas (não commitar `.env`)
2. Configure HTTPS com certificados válidos
3. Use uma imagem base atualizada
4. Implemente rate limiting e autenticação robusta
5. Use um reverse proxy (nginx) em frente

## 📊 Recursos das Imagens

### Backend
- Tamanho: ~200MB (multi-stage build otimizado)
- Baseia-se em: `mcr.microsoft.com/dotnet/aspnet:10.0`
- Runtime: .NET 10.0

### Frontend
- Tamanho: ~150MB (serve otimizado)
- Baseia-se em: `node:20-alpine`
- Build: npm
- Serve: serve package

## 🔄 CI/CD Integration

Para integrar com GitHub Actions:

```yaml
name: Docker Build & Push

on: [push]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: docker/setup-buildx-action@v2
      - uses: docker/build-push-action@v4
        with:
          context: ./ProjectManagerWebAPI
          push: true
          tags: ${{ secrets.DOCKER_REGISTRY }}/project-manager-backend:latest
```

## 📚 Referências

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET Container Images](https://hub.docker.com/_/microsoft-dotnet-aspnet/)
- [Node Alpine Images](https://hub.docker.com/_/node)
