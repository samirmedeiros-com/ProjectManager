# Portal de Consulta ao OpenSearch

Sub-app do monólito, em `/opensearch`. Ao contrário do SEUR e do OraConsole **não tem login
próprio**: reutiliza a autenticação do Project Manager e restringe o acesso ao **setor IT**.

## Acesso

- Rota Angular: `/opensearch`, com `AuthGuard` + `OpenSearchGuard`
- API: `/api/opensearch/*`, com `[Authorize]` + `[RequerSetor("IT")]`

O setor **não vai no JWT** (o token só transporta `sub`, `email`, `name`, `role`), por isso a
pertença é confirmada contra a base de dados a cada pedido, em `SetorAccessService`. O guard do
Angular é apenas conveniência de navegação — quem garante o acesso é o filtro na API.

Se o setor tiver outro nome na base de dados (por exemplo `TI`), mudar a constante
`OpenSearchController.SetorNecessario`.

Nota: **o Admin não tem bypass**. Quem não pertencer ao setor IT recebe 403, mesmo sendo Admin.

## Cluster

Cluster gerido OCI **AppLog**, OpenSearch 3.2.0, região eu-frankfurt-1. Tem **dois IPs privados
distintos**, e confundi-los custa tempo:

| | |
| --- | --- |
| **API OpenSearch** | `10.2.2.124:9200` ← é esta que a app usa |
| OpenSearch Dashboards | `10.2.2.9:5601` |

Ambos em **HTTPS**. O FQDN do endpoint
(`amaaaa….opensearch.eu-frankfurt-1.oci.oraclecloud.com`) **só resolve dentro da VCN**, por isso a
configuração usa o IP com `IgnorarCertificado: true`.

Para reconfirmar os IPs: `oci opensearch cluster get --opensearch-cluster-id <id>` devolve
`opensearch-private-ip` e `opendashboard-private-ip`. O `cluster list` **não** traz os IPs.

## Configuração

Secção `OpenSearch` no `appsettings.json`:

| Opção | Omissão | Função |
| --- | --- | --- |
| `BaseUrl` | — | API do OpenSearch (porta 9200), **não** a do Dashboards |
| `Utilizador` / `Password` | — | Basic auth |
| `IgnorarCertificado` | `true` | Certificado auto-assinado do cluster gerido |
| `TimeoutSegundos` | `30` | |
| `IncluirIndicesSistema` | `false` | Esconde os índices começados por ponto |
| `TamanhoPaginaMaximo` | `200` | Tecto para o `size` pedido pelo portal |

## Particularidades do OpenSearch tratadas no gateway

- O `_cat/indices` devolve **todos os números como string** — daí `bytes=b` e um parser tolerante.
- Campos `text` **não são ordenáveis** sem fielddata. O mapping expõe também o subcampo
  `.keyword`, e a UI redireciona a ordenação para ele.
- O achatamento do `_source` desce em objetos mas **deixa arrays inteiros numa célula** — achatar
  `tags.0`, `tags.1` geraria colunas diferentes por documento.
- A pesquisa por correlação (`RequestId`, `TraceId`) usa sempre o `.keyword`: um GUID pesquisado
  como `text` parte-se nos hífenes e traz documentos que só partilham um pedaço.

## Escala observada

1748 índices, ~749M documentos, 2,4 TB. Os índices são logs Serilog diários por serviço
(`logs-app-<serviço>-<data>`). O padrão `logs-app-*` abrange tudo: pesquisa em ~2,4 s, e o
`_mapping` agregado (2,3 MB) reduz a 221 campos únicos em ~0,8 s.

Uma pesquisa por `RequestId` sobre `logs-app-*` leva ~6,5 s por atravessar os 1748 índices —
restringir o padrão ou o intervalo de datas primeiro torna-a quase instantânea.

Ao diagnosticar "não devolve nada", confirmar primeiro com uma agregação `terms` que o valor
existe: `Level:Error` devolveu 0 num índice que só tem `Information`, e parecia um bug.
