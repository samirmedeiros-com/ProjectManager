# Mural de TV

Ecrã de parede, só leitura, sem login. Vive dentro do mesmo monólito — é mais uma
sub-app do Portal DPD, como o SEUR ou o OpenSearch.

## Endereço

```
http://<servidor>/tv?k=<chave>
```

A chave está em `TvDashboard:Chave` no `appsettings.json`. Sem chave, ou com a chave
errada, a API responde 401 e o ecrã diz apenas que o acesso é inválido.

Opcionalmente, `&tema=` muda o mural. Qualquer outro valor, ou a ausência do
parâmetro, mantém o escuro por omissão:

- `&tema=claro` — paleta clara, para paredes muito iluminadas onde o fundo
  escuro rebate luz.
- `&tema=transparente` — fundo do ecrã (e do `<body>`) transparente e sem o
  nome "Dashboard IT" no cabeçalho; pensado para sobrepor a outra fonte (ex.:
  captura de janela num video wall, browser source do OBS). Os cartões
  mantêm o seu próprio fundo opaco, só a base do ecrã fica transparente.

```
http://<servidor>/tv?k=<chave>&tema=claro
http://<servidor>/tv?k=<chave>&tema=transparente
```

## Configuração

```json
"TvDashboard": {
  "Ativo": true,           // false desliga o mural sem remover a rota (API devolve 404)
  "Chave": "...",          // segredo partilhado; longo e aleatório
  "RefreshSegundos": 60,   // o ecrã adota este valor na primeira resposta
  "LimiteLinhas": 8        // máximo de linhas por lista
}
```

## Como está protegido

- `Filters/RequerChaveTvAttribute.cs` valida a chave em `?k=` (ou no cabeçalho
  `X-Tv-Key`), com comparação de tempo constante. Chave vazia na configuração
  fecha o mural em vez de o abrir.
- Responde sempre `X-Robots-Tag: noindex, nofollow` e `Cache-Control: no-store`.
- O endpoint é **só de leitura**. Não expõe ids, emails nem nada que permita agir
  sobre os dados.
- Trocar a chave: mudar o `appsettings.json`, reiniciar a API e atualizar o URL na TV.

## Fontes de dados

O mural junta blocos de origens diferentes. Cada origem é um `ITvFonte` em
`Services/Tv/`, corre no seu próprio scope e é isolada nas falhas: se uma falhar,
só os cards dela saem do ecrã e o cabeçalho assinala-o.

| Fonte | Origem | Custo por ciclo |
|---|---|---|
| `seur` | `SEUR.SEUR_GUIA`, `SEUR.SEUR_ERRO`, `CHRONO_WEB.VERIFY_SEUR` | ~4s |
| `shpnot` | `GROUPSHPNOT.GEODT01SPN` | ~1,8s (ver índice abaixo) |
| `tteventos` | `DPDIT.GEODT01TT` | ~1s (ver índice abaixo) |
| `tracing` | `CHRONO_WEB.CW_PT_AS400_NEW_PORTAL` | ~4s (ver notas abaixo) |
| `as400` | `OPERACOES.HHP000` (AS400, jt400/JDBC) + Oracle | ~6s em regime (ver notas) |

A fonte `projetos` (`TvProjetosFonte`) existe no código mas **não está registada**: o
mural não tem cards dela e o registo custava ~1s de queries por ciclo, à toa. Para a
repor, acrescenta a linha `AddScoped<ITvFonte, TvProjetosFonte>()` no `Program.cs` e
declara os cards com `fonte: 'projetos'`.

Acrescentar uma fonte: escrever a classe, registá-la no `Program.cs` e declarar
os cards com `fonte: '<chave>'`.

Uma resposta é partilhada por todos os ecrãs durante metade do `RefreshSegundos`,
para várias TVs não multiplicarem a carga sobre tabelas de produção.

## Definir os cards

Tudo o que aparece no ecrã está declarado em
`ProjectManager/ProjectManagerWebUI/src/app/components/tv/tv-cards.ts`.
Cada entrada do array `CARDS` é um card: título, tipo, largura em colunas (a grelha
tem 12), altura em linhas, e uma função que escolhe o que mostrar a partir da resposta
da API. Acrescentar, remover ou reordenar cards é editar esse array — mais nada.

Tipos: `kpi` (número grande), `kpis` (dois ou três números no mesmo card), `barras`, `colunas` (série no tempo), `projetos`,
`tarefas`, `erros`, `comparativo` (o mesmo indicador em vários períodos).

## Responsivo: dois modos

O mesmo endereço serve a TV e o PC.

- **Modo mural** (largura ≥ 1100px **e** altura ≥ 700px): tudo cabe de uma vez,
  fixo à viewport, sem scroll. É o que aparece na TV.
- **Modo fluido** (tudo o resto): a página cresce com o conteúdo e faz scroll, os
  cards passam a duas colunas e depois a uma. Nada é cortado nem se sobrepõe, por
  mais estreita que a janela fique.

A condição do modo mural exige **largura e altura**: numa janela larga mas baixa,
manter o layout rígido voltaria a espremer o conteúdo até ao corte.

Três detalhes de implementação que não são óbvios:

- As dimensões dos cards vão para o template como **custom properties**
  (`--card-colunas`, `--card-linhas`), não como `grid-column`/`grid-row` diretos.
  Estilo inline ganha sempre às media queries, e é delas que depende o layout
  deixar de ser rígido.
- A media query dos 720px é **fechada em cima** (`max-width: 1099px`). Usa
  seletores de atributo (`[data-colunas='6']`), que têm mais especificidade do que
  a regra da grelha de 12 colunas e lhe ganhavam mesmo estando declarados antes.
- Os tamanhos de letra usam `clamp(min, vw, max)`. Só com `vw`, uma janela estreita
  torna o texto ilegível e uma TV grande torna-o desproporcionado.

**Secções**: o mural está dividido em faixas, uma por assunto, cada uma com o seu
cabeçalho e um risco a atravessar o ecrã — é isso que separa os blocos à distância,
mais depressa do que ler os títulos dos cards. As faixas estão declaradas em
`SECCOES`, no topo do `tv-cards.ts`:

| Secção | Cards | Linhas |
|---|---|---|
| **Operação SEUR** | Guias hoje · Enviadas ao Atlas · Guias com erro · Guias por hora — tudo numa fila (2+2+2+6) | 3 |
| **TTEventos** | Eventos hoje · Envios (sucesso e erro) · Fila por enviar · Enviados por hora — tudo numa fila (2+2+3+5) | 4 |
| **Envios SHPNOT** | Envios hoje · Duplicados MPSIDX · Destino · Hoje e ontem | 5 |
| **Tracing Público** | DPD Go · Portal · Hoje e ontem — tudo numa fila (3+3+6) | 5 |

**Orçamento de espaço**: é um só ecrã, sem scroll nem rotação. Dentro de cada
secção há uma grelha de **12 colunas**; as colunas de cada fila têm de somar 12 e
as linhas dos cards têm de somar o `linhas` que a secção declara. Acima disso o
conteúdo é cortado sem aviso, porque os cards têm `overflow: hidden`.

O `linhas` da secção é também o seu peso na altura: uma faixa de 6 linhas fica com
o dobro da altura de uma de 3. Para acrescentar um assunto novo, junta uma entrada
a `SECCOES` e baixa o `linhas` das outras.

`LimiteLinhas` no appsettings controla quantas linhas as listas trazem da API.

**Texto cortado**: os cards têm `overflow: hidden`, por isso o que não cabe
desaparece sem aviso. As regras que evitam isso:

- Rótulos **curtos**. Um card de 2 ou 3 colunas tem ~320–480px numa TV de 1080p;
  "Evento com mais parados" não cabe lá, "Evento com mais" cabe.
- Títulos de card e de grupo quebram até **duas linhas** (`-webkit-line-clamp: 2`)
  em vez de cortar com reticências — numa TV ninguém pode passar o rato por cima
  para ver o resto.
- Um card `kpis` com **três** valores encolhe sozinho (`[data-itens='3']`): número
  mais pequeno e rótulos a uma linha. Três valores não cabem com o tamanho de dois.
- **`table-layout: fixed`** nas tabelas dos grupos do comparativo. Sem ele a
  tabela não encolhe abaixo da largura mínima das células (que são `nowrap`) e os
  grupos da direita transbordam para fora do card — corte **horizontal**, que
  passa despercebido a quem só verifique a altura. Foi este o corte do SHPNOT.
- **Nada de texto dentro de um flex sem `flex: 0 0 auto`.** Os filhos de um
  contentor flex encolhem por defeito, e um texto encolhido é um texto cortado —
  era isto que cortava os títulos dos grupos ("Estado", "Envio"…) e os rótulos dos
  KPIs em janelas mais baixas.
- **O comparativo empilha os grupos abaixo de 1100px.** Quatro grupos a dividir
  900px dão 220px cada, e aí nenhum tamanho de letra salva "Duplicados MPSIDX" com
  dois números ao lado. Lado a lado só com espaço a sério; senão, em coluna.
- **Não abreviar rótulos para caber.** "Internac." lê-se exatamente como texto
  cortado. Se não cabe, o problema é de espaço e resolve-se no espaço.
- **`line-height` nunca abaixo de 1.15** nos números grandes. Com `line-height: 1`
  a caixa fica mais baixa do que o desenho da letra e os algarismos aparecem
  cortados em cima e em baixo — três ou quatro pixéis que se notam numa TV.
- O card sabe quanto espaço tem: `[data-linhas='2']` encolhe o número, porque um
  card de duas linhas de grelha não tem altura para o tamanho normal.
- **O `.mural` é `position: fixed; inset: 0`**, e não `height: 100vh`. O `<body>`
  da aplicação tem a margem de 8px por defeito, que o reset deste componente não
  alcança por estar encapsulado; com `100vh` o mural começava a 8px e acabava a
  1088, e a última faixa ficava encostada ao limite com ar de cortada.
- A escala global está em `--escala: 0.85` no `:host`. Com quatro secções no ecrã,
  este é praticamente o limite do que se lê a quatro metros — se acrescentares uma
  quinta secção, é mais honesto voltar à rotação de páginas do que continuar a
  encolher a tipografia.

**Verificar antes de dar por feito**: a olho nu, num portátil, não se vê o que vai
acontecer na TV. Há um script que abre o mural em várias resoluções e lista o que
transborda:

```
npm install playwright
node tools/verificar-mural.js "http://localhost:4200/tv?k=<chave>"
```

Correr sempre depois de mexer no `tv-cards.ts` ou no SCSS.

## Notas sobre a GEODT01SPN

49 milhões de linhas, 50 GB, sem partições. Tudo o que o mural precisa sai de
**uma só passagem** sobre a janela de dois dias — a query deriva todas as métricas
em ramos `CASE` em vez de repetir a varredura por indicador.

Foi criado o índice de cobertura `IDX_GEODT01SPN_TV` (~3,5 GB) sobre
`DATAHORA_INSERT, FLAGENV, DATAHORAENV, SPTDATTIMX, SCOUNTRYCX, RCOUNTRYCX, MPSIDX`,
para a query se resolver no índice sem os ~100 mil saltos à tabela que o plano
anterior fazia. Medido: **6,8s → 1,8s**, e o `TABLE ACCESS BY INDEX ROWID BATCHED`
desapareceu do plano. Ocupa 3,55 GB e tem 49,7 M entradas.
Reverter é `DROP INDEX GROUPSHPNOT.IDX_GEODT01SPN_TV`.

Foi criado com `ONLINE PARALLEL 4 LOGGING` (e `NOPARALLEL` no fim, para o grau de
paralelismo não contaminar os planos de outras queries). `LOGGING` é obrigatório
aqui: a base tem um standby ativo e `FORCE_LOGGING` ligado.

Três coisas que os dados mostram e convém saber ao ler o card:

- **`SPTDATTIMX` tem valores fora do formato** `YYYYMMDDHH24MISS` — cerca de 1,6%
  num dia normal, com aspeto `8-206-20022043`. Contam à parte, em "formato
  inválido", para não serem confundidos com envios fora do dia.
- **`DATAHORAENV` é sempre do mesmo dia da inserção** nos dados observados, por
  isso "enviados noutro dia" tende a zero. A métrica existe para apanhar o desvio
  quando ele acontecer.
- **Nacional/internacional conta cada `MPSIDX` uma só vez.** Um envio duplicado na
  tabela não são dois envios no mundo real; nacional + internacional dá o total de
  únicos, não o total de linhas.

## Notas sobre a GEODT01TT (TTEventos)

27,8 milhões de linhas, 26 GB, sem partições. Uma só passagem sobre a janela de
dois dias serve tudo: agrupa por dia **e** pela hora de envio, o que dá ~50 linhas
de resultado a partir das quais se derivam os totais e o gráfico horário.

Semântica dos campos, confirmada nos dados:

- **`FLAGENV`**: `Y` enviado com sucesso, `E` erro no envio, `N` ainda na fila.
- **`DATAFLAG_ENV`** é o carimbo de quando o envio foi tentado — preenchido nos `Y`
  e nos `E`, nulo nos `N`. É este, e não o `DATAHORA_INSERT`, que diz a que horas as
  coisas saíram. Cuidado: **`DATAHORA_ENVIO` existe mas está sempre nula**, e
  `DATAENV`/`HORAENV` são números que se referem ao evento, não ao envio.
- A **taxa de sucesso** mede-se sobre o que já foi tentado (`Y + E`). Contar a fila
  `N` como insucesso faria a percentagem cair só por haver trabalho pendente, que é
  o estado normal a meio do dia.

No gráfico horário, a barra fica **vermelha nas horas em que houve erros** — assim
vê-se de relance *quando* correu mal, não só que o dia tem erros.

**A fila por enviar conta-se sobre a tabela inteira**, e não sobre a janela de dois
dias: um evento encravado há uma semana é exatamente o que interessa ver, e seria o
primeiro a escapar a uma janela curta. Dela saem três números — quantos estão
parados, há quanto tempo espera o mais antigo (e a que horas chegou), e qual o tipo
de evento (`SCANCODEX`) com mais parados. A repartição completa por tipo vem em
`fila.porEvento` (top 5), pronta para um card de barras se quiseres mostrá-la.

O atraso fica **amarelo aos 30 minutos e vermelho aos 60** — é a partir daí que a
fila deixa de parecer um lote normal e começa a parecer um bloqueio.

Índice de cobertura `IDX_GEODT01TT_TV` sobre `DATAHORA_INSERT, FLAGENV,
DATAFLAG_ENV`, pelo mesmo motivo do SHPNOT: o plano anterior fazia
`TABLE ACCESS BY INDEX ROWID` sobre ~518 mil linhas. Ocupa **0,88 GB** com 28,0 M
entradas, e o custo do plano caiu de **103K para 2190**.
Reverter é `DROP INDEX DPDIT.IDX_GEODT01TT_TV`.

Segundo índice, `IDX_GEODT01TT_FILA` sobre `FLAGENV, SCANCODEX, DATAHORA_INSERT`
(~0,7 GB): serve a consulta da fila, que filtra por flag e não por data, e por isso
não aproveita o índice anterior. Sem ele, agrupar por `SCANCODEX` obriga a ~39 mil
saltos à tabela e o mural passa de ~4s para 7s.
Reverter é `DROP INDEX DPDIT.IDX_GEODT01TT_FILA`.

**Estatísticas**: em 21/08/2026 as estatísticas da tabela eram de 8 de agosto e
estimavam **310 linhas** para `FLAGENV='N'` quando eram dezenas de milhares — nessa
altura a fila estava vazia. Foram recolhidas de novo nesse dia
(`GATHER_TABLE_STATS` com `AUTO_SAMPLE_SIZE`, `cascade => TRUE`, grau 4) e a
estimativa passou a acompanhar a realidade.

Se algo na `GEODT01TT` ficar mais lento por causa da mudança de planos, as
estatísticas anteriores podem ser repostas com `DBMS_STATS.RESTORE_TABLE_STATS`,
que o Oracle guarda automaticamente.

## Notas sobre a CW_PT_AS400_NEW_PORTAL (Tracing Público)

**398 milhões de linhas, 232 GB** — de longe a maior tabela do mural. Duas
armadilhas que custaram caro a descobrir:

1. **`DATAINSERT` não serve de janela**: é nulo em **265 dos 398 milhões** de linhas.
   Filtrar por lá varre o índice quase todo e a consulta não devolve. A coluna de
   data útil é a **`HHPDATINC`**, um número em formato `YYYYMMDD`, com índice próprio
   (`IDX_NEW_PORTAL5`) e um composto com a flag do portal (`IDX_NEW_PORTAL4`).
2. **`MIN` e `MAX` na mesma consulta** anulam a otimização de pontas do índice e
   passam a full scan. Nesta tabela isso não devolve — é preciso separá-los.

Significado das flags, confirmado nos dados e não na documentação:

- **`FLAGDPDGO`**: `'Y'` foi enviado ao DPD Go — e tem sempre `DATAHORA_DPDGO`
  preenchido; **nulo** está pendente, e nunca tem carimbo. **Não existe `'N'`** nesta
  coluna, ao contrário das outras flags do sistema.
- **`HHPCONFIRM`**: `'Y'` enviado ao Portal, `'N'` pendente, `'E'` erro.

As duas flags repartem o mesmo total do dia, cada uma à sua maneira — somam ambas o
número de linhas de `HHPDATINC`.

## AS400 vs Oracle (fonte `as400`)

Compara o que existe no AS400 com o que já chegou ao Oracle, por dia, pelo campo
`HHPDAT` que ambos os lados têm — o dia corrente e os três anteriores.

**Os `HHPROWID` repetidos são excluídos dos dois lados**, e isso não é preciosismo:
o AS400 tem repetidos (umas dezenas por dia) e o Oracle não. Comparar totais em
bruto mostraria uma diferença que não existe. Medido a 21/08/2026, os únicos batiam
ao registo nos dias fechados — 974 920, 819 826 e 785 429 nos dois lados —, e a
única diferença estava no dia corrente, que é o atraso normal da replicação.

**Custo e cache**: o dia corrente custa ~5s no AS400 e ~1s no Oracle; os quatro dias
de uma vez custam ~43s no AS400. Como os dias fechados já não mudam, são pedidos uma
vez e guardados até ao fim do dia — só o dia corrente é recalculado a cada ciclo.

### Ligação ao AS400 — atenção no deploy

A ligação usa o **jt400**, o driver JDBC oficial da IBM, compilado para .NET pelo
**IKVM**. O `jar` está em `lib/jt400.jar` e é referenciado no `.csproj`:

```xml
<PackageReference Include="IKVM" Version="8.15.0" />
<IkvmReference Include="lib/jt400.jar" AssemblyName="jt400"
               AssemblyVersion="20.0.7.0" AssemblyFileVersion="20.0.7.0" />
```

Configuração na secção `As400` do appsettings:

```json
"As400": { "Host": "192.168.239.26", "Utilizador": "…", "Password": "…" }
```

**Porque não ODBC.** A alternativa natural era ODBC com a connection string que o
deploy já tem (`Driver={IBM i Access ODBC Driver};System=…`). Foi implementada e
descartada: obriga a ter **unixODBC ≥ 2.3.1** e o **IBM i Access ODBC Driver**
instalados na máquina onde a API corre, e sem eles a fonte falha com
`Dependency unixODBC ... is required`. Com o jt400 embutido não há nada a instalar
no servidor, e o comportamento é o mesmo em desenvolvimento e em produção.

Outros caminhos tentados e postos de lado, para não serem retentados:

- **Database links do Oracle** (`AS400`, `CHRONO_WEB.ORADW03_AS400`): estão
  obsoletos, dão `ORA-12154` — o host não resolve a partir do servidor.
- **`Net.IBM.Data.Db2`** por DRDA na porta 446: liga ao AS400 mas recusa com
  `SQL1598N`, por exigir licença DB2 Connect.

O AS400 responde nas portas 8471 (usada pelo jt400) e 446, a partir da rede interna.
O nome da base de dados relacional é `S06891C4`, se alguma vez for preciso.

**Se um dia isto correr no OKE, o pod tem de ficar no pool `poolGateway`.** O AS400
só é alcançável a partir desse pool: os dezoito deployments do `DeployDPDOracleOCI`
que falam com ele carregam todos a mesma `nodeAffinity`, e nenhum outro. Já está em
`kubernetes/backend/deployment.yaml`. Sem ela a fonte não rebenta — devolve
`As400Disponivel = false` e o ecrã diz que a comparação está cega —, mas o card
nunca mostra dados.

### Quando uma fonte falha

O card **fica no ecrã** a dizer «Sem ligação», em vez de desaparecer. Um mural que
muda de disposição a cada falha obriga a reprocurar tudo de cada vez que se olha
para ele. O que nunca acontece é mostrar zeros: um zero falso é pior do que um
card ausente, porque ninguém desconfia dele.

No template, `semLigacao(card)` tem de ser testado **antes** de `vazio(card)` e de
qualquer função de leitura — sem a fonte, `card.dados()` rebentaria a ler `undefined`.

«Sem ligação» é diferente de «Sem dados»: o segundo é uma resposta («hoje não houve
envios»), o primeiro é a ausência de resposta. Fica em tom de aviso apagado, porque
é uma falha do mural e não um alarme da operação — para isso já há as etiquetas
vermelhas no topo com as fontes em falta.

### Enviados por hora (SHPNOT) — porque o filtro não é pela hora de envio

A pergunta é o ritmo de saída, mas a query **filtra por `DATAHORA_INSERT`** e só
depois agrupa pela hora de `DATAHORAENV`. A razão: `DATAHORAENV` **não está
indexada**, e uma query filtrada por ela varre os 50 GB da tabela — foi cancelada
aos 300 s. Pela janela indexada custa ~114 ms, medido.

A troca só é legítima porque **nenhuma linha é enviada num dia diferente daquele em
que entra**: o card «Envio / Noutro dia» do próprio mural mostra zero. É esse o
número a vigiar se um dia o somatório das horas deixar de bater certo com o
«Sucesso (Y)» do dia.

### Configuração embutida no código

A chave do mural e a ligação ao AS400 têm **valores de origem no próprio código**
(`TvDashboardOptions.ChavePorOmissao` e as constantes no topo de `TvAs400Fonte`).
A razão é o deploy: a publicação é copiada para o servidor, onde o `appsettings.json`
é o anterior ao mural e não tem as secções `TvDashboard` nem `As400`. Sem valores de
origem, o mural chegaria a produção a responder 401 a tudo e com a comparação cega.

O `appsettings.json` continua a sobrepor-se quando traz essas secções — é assim que
se muda a chave sem recompilar. Verificado a 21/08/2026: com as duas secções
removidas do `appsettings.json` publicado, o `ping` aceita a chave do código e a
fonte do AS400 liga (`As400Disponivel = true`).

## Diagnosticar lentidão

O agregador escreve no log o tempo de cada fonte a cada ciclo:

```
[TV] Fonte 'tteventos': 1194ms
[TV] Fonte 'seur': 4522ms
[TV] Fonte 'shpnot': 5075ms
```

O tempo do mural é o da fonte mais lenta, não a soma — elas correm em paralelo.
Atenção ao ler estes números: **uma fonte medida isoladamente é mais rápida do que
dentro do mural**, porque as quatro competem pela mesma base. A `shpnot` custa ~1s
sozinha e ~6s em conjunto. Isso não é um defeito da fonte, é o preço do paralelismo
— que continua a compensar, porque a alternativa seria a soma de todas.

Os números também crescem ao longo do dia: a janela é de dois dias, por isso às 10h
tem mais linhas do que às 8h. Antes de suspeitar de uma regressão, compara com o
volume da janela na mesma altura.

## Notas de operação

- O ecrã guarda os últimos dados bons: se a rede falhar, mostra um aviso no topo em
  vez de apagar o mural, e o carimbo de hora deixa de avançar.
- Um erro de rede não parte o ciclo de atualização — a falha é apanhada dentro do
  fluxo e o mural volta sozinho quando a API responder.
- O tema é escuro fixo, sem seguir o sistema: a TV não tem quem escolha o tema.
- Se o ecrã ficar longe, subir `--escala` no `:host` do `tv.component.scss`.
