# Gestão Kubernetes

Sub-app do monólito, em `/kubernetes`. Lista os deployments dos namespaces `gateway`, `webapi`,
`qualidade` e `georouting`, abre os pods de cada um no `+` da linha e permite **parar**,
**arrancar** e **reiniciar** cada deployment.

Nada disto é uma aplicação separada: o backend são controladores em `ProjectManagerWebAPI`, o
frontend são componentes em `ProjectManagerWebUI`, e a entrada é um cartão no Portal DPD.

## Acesso — credenciais próprias

Ao contrário do portal de OpenSearch, esta aplicação **tem login próprio**, como o SEUR e o
OraConsole: tabela `KubernetesUsers`, ecrã em `/login-kubernetes`, chaves de `localStorage`
`k8s_token` / `k8s_user`. Um utilizador do Project Manager **não entra aqui**.

| | |
| --- | --- |
| Rota Angular | `/login-kubernetes`, `/kubernetes` (com `KubernetesAuthGuard`) |
| API de autenticação | `/api/kubernetes/auth/*` |
| API do cluster | `/api/kubernetes/*`, com `[Authorize]` + `[RequerApp("kubernetes")]` |

**Todas as aplicações do portal assinam o JWT com a mesma chave, issuer e audience**, por isso
`[Authorize]` sozinho aceitaria um token do SEUR ou do Project Manager. Quem separa as credenciais
depois do login é o claim `app` e o filtro `RequerAppAttribute`. (O SEUR emite o claim mas não o
verifica — este é o defeito que aqui não se repetiu.)

Papéis, verificados por `RequerPapelAttribute`:

| Papel | Vê o cluster | Parar/arrancar/reiniciar | Gere utilizadores |
| --- | --- | --- | --- |
| `Admin` | sim | sim | sim |
| `Operador` | sim | sim | não |
| `Leitor` | sim | **não** | não |

Utilizador semeado no arranque (`SeedKubernetesAdmin`): `admin@kubernetes.local` / `k8s2026`.
**Alterar no primeiro acesso.**

### Gestor de utilizadores

Em `/kubernetes/utilizadores` (só Admin): nome, email e perfil. **A password nunca é escrita por
ninguém** — é gerada pelo servidor e enviada por email, junto com o endereço de acesso, que está
em `Kubernetes:UrlLogin` (`http://projetos.dpd.pt/login-kubernetes`). Fica em configuração porque
é o endereço público do portal, que não coincide com o do servidor onde a aplicação corre.

Se o envio do email falhar, o utilizador **fica na mesma criado** e a password aparece no ecrã ao
administrador para entregar por outra via — perdê-la obrigaria a repor logo a seguir. O botão
"Enviar password" gera uma nova e invalida a anterior.

## Informação por deployment

O ícone **i** ao lado do nome abre um popup com **título** (máx. 100 caracteres) e **memo**.
O título, quando existe, aparece por baixo do nome na lista, e o ícone fica cheio — distingue-se
"há aqui algo para ler" de "ninguém escreveu nada" sem ter de abrir nada.

Vive na tabela `KubernetesDeploymentNotas`, não em anotações do cluster: é conhecimento da equipa,
não configuração, e um redeploy não o pode levar à frente. Índice único em
(namespace, deployment) — sem ele, dois gravares em simultâneo criavam duas notas.

Escrever exige o mesmo perfil dos comandos (Admin ou Operador); o Leitor abre e lê.

**Armadilha:** as colunas de texto livre são **NCLOB, não CLOB**. O CLOB guarda no charset da base
de dados, que aqui não é Unicode — um travessão "—" chegava ao registo como "¿". E o Oracle **não
converte** CLOB em NCLOB com ALTER (ORA-22859): a migração `NotasEmNclob` apaga e recria as
colunas.

## Registo de ações

Tabela `KubernetesAuditLogs`. Regista **entradas, entradas recusadas, parar, arrancar, reiniciar e
alterações à informação**, com quem, quando, sobre quê, o resultado e o IP de origem.

Nas alterações à informação guardam-se as colunas `ValorAnterior` e `ValorNovo` — o registo
responde sozinho a "o que é que mudou", em vez de dizer só que alguém mexeu. Gravar sem alterar
nada não gera registo. Na tabela, essas linhas trazem um "ver a alteração" que abre o antes e o
depois lado a lado.

- **Vista global**: `/kubernetes/registo`, com filtros por ação e por utilizador. **Só Admin** —
  inclui as entradas de toda a gente.
- **Por deployment**: botão **Logs** em cada linha da lista, num popup. **Qualquer utilizador da
  aplicação** o vê: saber quem parou o quê é precisamente o que se quer transparente.

Ambas as vistas são a mesma tabela (`AuditoriaTabelaComponent`), do mais recente para o mais
antigo e paginada.

Decisões que valem a pena reter:

- O **nome e o email ficam copiados para a linha**, não só a chave estrangeira. Um utilizador
  desativado ou renomeado não pode reescrever o passado.
- **As falhas também se registam** — uma tentativa recusada é tão relevante como uma que passou.
  Um login falhado guarda a razão real ("Password inválida", "Email desconhecido") mesmo que ao
  utilizador se diga sempre a mesma coisa.
- **Falhar a gravar o registo não desfaz a ação**: já aconteceu no cluster. Fica no log da
  aplicação e o utilizador recebe o sucesso que de facto teve.
- A ordenação desempata por `Id`, senão duas linhas gravadas no mesmo instante fariam a
  paginação repetir ou saltar uma.
- O IP vem do `X-Forwarded-For` quando existe: atrás do ingress, o endereço direto é o do proxy.

## Ligação ao cluster

O backend fala com o servidor de API do OKE por HTTPS + JSON, sem cliente oficial — o mesmo
desenho do `OpenSearchGateway`. **Não usa o kubeconfig da OCI**: esse autentica por `exec` do CLI
`oci`, que não existe no servidor da aplicação e devolve um token de vida curta que ninguém ali
saberia renovar.

Em vez disso há uma ServiceAccount dedicada, em `kubernetes/rbac/projectmanager-k8s.yaml`:

```bash
kubectl apply -f kubernetes/rbac/projectmanager-k8s.yaml

# o token para o appsettings:
kubectl -n deploys get secret projectmanager-k8s-token -o jsonpath='{.data.token}' | base64 -d
```

O RBAC é dado **namespace a namespace** (Role + RoleBinding, sem ClusterRole), com `get`/`list`
em pods, deployments e replicasets e `patch`/`update` em deployments — não há `create` nem
`delete`, e fora dos quatro namespaces a ServiceAccount não vê nada.

Configuração, em `appsettings.json`:

```json
"Kubernetes": {
  "BaseUrl": "https://144.24.183.60:6443",
  "Token": "",
  "IgnorarCertificado": true,
  "TimeoutSegundos": 30,
  "Namespaces": ["gateway", "webapi", "qualidade", "georouting"],
  "PermitirComandos": true
}
```

- `Token` fica **vazio no repositório** — preencher na instalação, ou passar por variável de
  ambiente `Kubernetes__Token`.
- `Namespaces` é lista branca a sério: o `KubernetesGateway` recusa qualquer outro namespace antes
  do pedido sair da aplicação, e a API responde 404 (não 403 — dizer "sem permissão" confirmaria
  que o namespace existe).
- `PermitirComandos: false` deixa a aplicação em leitura, sem mexer no RBAC.
- O namespace chama-se **`georouting`**, não `geo`.

## O que os três comandos fazem

O Kubernetes não tem "pausa" de um deployment no sentido de suspender um serviço
(`rollout pause` só congela os rollouts, os pods continuam a servir), por isso:

| Botão | O que acontece |
| --- | --- |
| **Parar** | escala a `0`, guardando as réplicas atuais na anotação `projectmanager.dpd.pt/replicas-antes-de-parar` |
| **Arrancar** | repõe as réplicas guardadas (1 se não houver registo) e limpa a anotação |
| **Reiniciar** | carimba `kubectl.kubernetes.io/restartedAt` no template — igual a `kubectl rollout restart` |

Sem a anotação, arrancar teria de adivinhar o número e um serviço de 4 réplicas voltaria com 1.

Os três são `POST` e passam por um diálogo de confirmação no portal. Cada pedido fica registado no
log da API com o `sub` de quem o executou.

## Armadilhas

- **O patch tem de ir com `application/strategic-merge-patch+json`.** Com `application/json` o
  servidor espera um JSON Patch (lista de operações) e responde 415/400. Apagar uma anotação
  faz-se pondo-a a `null` — é assim que o strategic merge remove chaves.
- **Os pods pedem-se pelo `matchLabels` do deployment**, não pelo nome: o nome do pod traz o hash
  do ReplicaSet e muda a cada rollout.
- **A `phase` de um pod em CrashLoopBackOff continua a ser `Running`.** A razão real está no
  `state.waiting.reason` do contentor — é essa que a coluna Estado mostra.
- **Tokens projetados (TokenRequest) não servem aqui**: expiram e só se renovam dentro de um pod.
  Daí o Secret do tipo `kubernetes.io/service-account-token` no manifesto.
- **`RequerApp` devolve 401 e não 403.** O problema não é falta de permissão, é estar autenticado
  na aplicação errada; o 401 leva o interceptor do Angular ao login desta aplicação.
- **O `auth.interceptor` exclui `/api/kubernetes/`**, senão punha aqui o token do Project Manager.
  Quem injeta o Bearer certo é o `kubernetes.interceptor`.
- Os tokens de acesso ao cluster e os das aplicações são coisas diferentes: o primeiro é da
  ServiceAccount e vive no `appsettings`; o segundo é o JWT do utilizador.

## Ficheiros

**Backend** — `Controllers/KubernetesController.cs`, `Controllers/KubernetesAuthController.cs`,
`Services/KubernetesGateway.cs`, `Services/KubernetesOptions.cs`, `Services/KubernetesAuthService.cs`,
`Services/KubernetesAuditService.cs`, `Filters/RequerAppAttribute.cs`, `Models/KubernetesModels.cs`,
`Services/KubernetesNotaService.cs`, `Models/KubernetesUser.cs`, `Models/KubernetesAuditLog.cs`,
`Models/KubernetesDeploymentNota.cs`, `DTOs/KubernetesDtos.cs`, `SeedKubernetesAdmin.cs`,
migrações `AddKubernetesUsers`, `AddKubernetesAuditLog`, `AddKubernetesNotas` e `NotasEmNclob`.

**Frontend** — `components/kubernetes/`, `components/login-kubernetes/`,
`components/kubernetes-auditoria/`, `components/kubernetes-utilizadores/`,
`components/kubernetes-shared/` (menu comum às três páginas),
`services/kubernetes.service.ts`, `services/kubernetes-auth.service.ts`,
`services/kubernetes.interceptor.ts`, `guards/kubernetes-auth.guard.ts`.

**Cluster** — `kubernetes/rbac/projectmanager-k8s.yaml`.
