# 🚀 Kubernetes Deployment - Project Manager

Guia completo para fazer deploy do ProjectManager no Oracle Kubernetes Engine (OKE).

## 📋 Pré-requisitos

1. **Oracle OKE Cluster** configurado e acessível
2. **GitHub Secrets** configurados corretamente
3. **kubectl** instalado localmente (para testes manuais)
4. **Docker** instalado (para builds locais)

## 🔐 GitHub Secrets Necessários

Configure os seguintes secrets no repositório GitHub:

| Secret | Descrição | Exemplo |
|--------|-----------|---------|
| `OCI_CLI_AUTH_TOKEN` | Token de autenticação OCI | `qUqx;>a>W17ObHyiTaO:` |
| `OCI_CLI_FINGERPRINT` | Fingerprint da chave API | `e8:6d:78:a5:5f:60:5b:82:6d:af:35:e8:c1:1c:ba:43` |
| `OCI_CLI_REGION` | Região OCI | `eu-frankfurt-1` |
| `OCI_CLI_TENANCY_OCID` | OCID do Tenancy | `ocid1.tenancy.oc1...` |
| `OCI_CLI_USER_OCID` | OCID do Usuário | `ocid1.user.oc1...` |
| `OCI_CLI_KEY_CONTENT` | Conteúdo da chave privada PEM | (conteúdo da chave em base64 ou texto) |
| `OCI_TENANCY_NAMESPACE` | Namespace do Tenancy | `frsor8yuh1au/oracleidentitycloudservice` |
| `OCI_USERNAME` | Usuário OCI | `samir.medeiros@dpd.pt` |
| `OKE_CLUSTER_OCID` | OCID do cluster OKE | `ocid1.cluster.oc1...` |
| `OCI_TENANCY` | Nome do Tenancy | `frsor8yuh1au` |

### Como adicionar Secrets:

1. Acesse: GitHub → Settings → Secrets and Variables → Actions
2. Clique em "New repository secret"
3. Adicione cada secret com seu valor correspondente

## 📁 Estrutura de Arquivos

```
ProjectManager/
├── .github/workflows/
│   ├── deploy-backend.yml       # Workflow para backend
│   ├── deploy-frontend.yml      # Workflow para frontend
│   └── KUBERNETES_DEPLOYMENT.md # Este arquivo
├── kubernetes/
│   ├── backend/
│   │   ├── deployment.yaml      # Deployment do backend
│   │   └── service.yaml         # Service do backend
│   ├── frontend/
│   │   ├── deployment.yaml      # Deployment do frontend
│   │   ├── service.yaml         # Service do frontend
│   │   └── ingress.yaml         # Ingress do frontend
│   └── kustomization.yaml       # Configuração Kustomize (opcional)
```

## 🚀 Como Fazer Deploy

### Via GitHub Actions (Recomendado)

#### 1. Deploy do Backend

1. Vá para: **Actions** → **Deploy Backend - ProjectManager**
2. Clique em **Run workflow**
3. Preencha os inputs:
   - **Ambiente**: `staging` ou `production`
   - **Image Tag**: `latest` ou uma tag específica (ex: `v1.0.0`)
4. Clique em **Run workflow**

#### 2. Deploy do Frontend

1. Vá para: **Actions** → **Deploy Frontend - ProjectManager**
2. Clique em **Run workflow**
3. Preencha os inputs:
   - **Ambiente**: `staging` ou `production`
   - **Image Tag**: `latest` ou uma tag específica (ex: `v1.0.0`)
4. Clique em **Run workflow**

O workflow irá:
- ✅ Build da imagem Docker
- ✅ Push para OCIR (Oracle Container Image Registry)
- ✅ Deploy no OKE com os manifestos Kubernetes
- ✅ Aguardar e validar o rollout

## 📊 Monitorar Deployment

### Ver status dos Pods

```bash
# Setup kubeconfig (automatizado no workflow)
oci ce cluster create-kubeconfig --cluster-id <CLUSTER_OCID> --file ~/.kube/config

# Verificar Pods
kubectl get pods -n staging
kubectl get pods -n production

# Ver detalhes de um pod
kubectl describe pod <POD_NAME> -n staging

# Ver logs
kubectl logs -f <POD_NAME> -n staging
```

### Ver Services

```bash
# Listar services
kubectl get svc -n staging
kubectl get svc -n production

# Obter IP externo do frontend
kubectl get svc project-manager-frontend -n staging
```

### Monitorar Eventos

```bash
# Ver eventos recentes
kubectl get events -n staging --sort-by='.lastTimestamp'

# Watch em tempo real
kubectl get pods -n staging -w
```

## 🔄 Rollback de Deployment

Se algo der errado, você pode fazer rollback:

```bash
# Ver histórico de rollouts
kubectl rollout history deployment/project-manager-backend -n staging

# Fazer rollback para versão anterior
kubectl rollout undo deployment/project-manager-backend -n staging

# Fazer rollback para revisão específica
kubectl rollout undo deployment/project-manager-backend -n staging --to-revision=2
```

## 🛠️ Customizações

### Alterar Réplicas

Edite `kubernetes/backend/deployment.yaml` ou `kubernetes/frontend/deployment.yaml`:

```yaml
spec:
  replicas: 3  # Mude de 2 para 3
```

### Alterar Recursos (CPU/Memory)

```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

### Configurar Ingress para Produção

Edite `kubernetes/frontend/ingress.yaml`:

```yaml
- host: projectmanager.example.com  # Mude para seu domínio
  http:
    paths:
    - path: /
      pathType: Prefix
      backend:
        service:
          name: project-manager-frontend
          port:
            number: 80
```

## 🔒 Security Best Practices

1. **Não commitar secrets** no repositório
2. **Usar GitHub Secrets** para credenciais sensíveis
3. **Restringir acesso ao cluster** com RBAC
4. **Usar Network Policies** para restringir tráfego
5. **Implementar Pod Security Policies**
6. **Usar Liveness e Readiness Probes** (já configurados)
7. **Definir Resource Limits** (já configurados)

## 📈 Escalabilidade

### Auto-scaling de Pods (HPA)

Para ativar auto-scaling horizontal:

```bash
kubectl autoscale deployment project-manager-backend \
  -n staging \
  --min=2 \
  --max=5 \
  --cpu-percent=80
```

### Pod Disruption Budget

Para garantir disponibilidade durante updates:

```yaml
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: project-manager-backend-pdb
spec:
  minAvailable: 1
  selector:
    matchLabels:
      app: project-manager-backend
```

## 🐛 Troubleshooting

### Pod não inicia

```bash
# Ver logs
kubectl logs <POD_NAME> -n staging

# Ver eventos
kubectl describe pod <POD_NAME> -n staging

# Ver recursos
kubectl top pod <POD_NAME> -n staging
```

### Erro de conexão entre serviços

```bash
# Testar conectividade
kubectl run -it --rm debug --image=busybox --restart=Never -- sh
nslookup project-manager-backend.staging.svc.cluster.local
```

### Image Pull Error

```bash
# Verificar se o secret de registry está configurado
kubectl get secrets -n staging

# Criar secret se necessário
kubectl create secret docker-registry ocir-secret \
  --docker-server=eu-frankfurt-1.ocir.io \
  --docker-username=frsor8yuh1au/samir.medeiros@dpd.pt \
  --docker-password=<TOKEN> \
  -n staging
```

## 📚 Referências

- [Oracle Kubernetes Engine Documentation](https://docs.oracle.com/en-us/iaas/Content/ContEng/home.htm)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [kubectl Cheat Sheet](https://kubernetes.io/docs/reference/kubectl/cheatsheet/)
- [OCI CLI Reference](https://docs.oracle.com/en-us/iaas/tools/oci-cli/latest/)

## 💡 Dicas

1. **Teste localmente primeiro** com Docker Compose
2. **Use staging** para testar antes de produção
3. **Versione suas imagens** com tags semânticas (v1.0.0)
4. **Monitore os logs** regularmente
5. **Faça backups** do banco de dados regularmente
6. **Revise secrets** periodicamente e atualize

## 📞 Suporte

Para problemas ou dúvidas:
1. Verifique os logs do workflow no GitHub Actions
2. Consulte os eventos do Kubernetes
3. Revise os secrets e configurações
4. Verifique a documentação oficial do OKE
