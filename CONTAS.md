# Gestão de Dados — contas e subcontas

Sub-app do Project Manager para consultar, alterar e criar contas e subcontas do AS400
(`WSDPD.AS400_CONTAS` e `WSDPD.AS400_SUBCONTAS`) e enviá-las ao portal de clientes,
escolhendo o ambiente entre produção e qualidade.

Substitui, para o trabalho pontual, o que o `ContasPortal_NET` faz em ciclo: essa aplicação
de linha de comandos varre as contas com `FLAGPORTAL = 'N'` e envia-as sozinha. Aqui é uma
pessoa a abrir uma conta, corrigir um campo e mandar — sem esperar pelo ciclo e sabendo o
que o portal respondeu.

## Acesso

Não tem login próprio: usa as **credenciais da Gestão SEUR**, como a Consulta OpenSearch.

Todas as aplicações do portal assinam o JWT com a mesma chave, issuer e audience, por isso
`[Authorize]` sozinho aceitaria o token de qualquer uma delas. Quem separa é o claim `app`,
verificado por `[RequerApp("seur")]` em `Controllers/ContasController.cs`.

Duas consequências práticas, iguais às do OpenSearch:

- `auth.interceptor.ts` **exclui** `/api/contas/` — injetar aí o Bearer do Project Manager
  mandaria a credencial errada. O `contas.service.ts` põe o `seur_token` à mão.
- O `contas.guard.ts` manda quem não tem sessão para `/login-seur?returnUrl=/contas`.

## Estrutura

Backend (`ProjectManagerWebAPI/`):

| Ficheiro | Papel |
|---|---|
| `Controllers/ContasController.cs` | `/api/contas` — listar, ler, gravar, enviar |
| `Services/ContasRepository.cs` | SQL sobre WSDPD; o SQL é gerado a partir do mapa de colunas |
| `Services/ContasPortalSender.cs` | Token OAuth e envio ao portal, por ambiente |
| `Services/ContasOptions.cs` | Secção `ContasPortal` do `appsettings.json` |
| `Models/ContasModels.cs` | `Conta`, `SubConta` e o atributo `[Coluna]` |
| `Models/ContasPortalDtos.cs` | Payload aceite pelo portal (copiado do `ContasPortal_NET`) |

Frontend (`ProjectManager/ProjectManagerWebUI/src/app/`):
`components/contas/`, `services/contas.service.ts`, `guards/contas.guard.ts`,
rota `/contas` e cartão no Portal DPD.

## Decisões que não são óbvias no código

**Gravar repõe `FLAGPORTAL = 'N'`.** As tabelas não são desta aplicação: são alimentadas
pelo AS400 e lidas pelo processo automático. Uma alteração feita aqui volta à fila de envio
e sai na mesma pelo ciclo, mesmo que ninguém carregue no botão.

**O envio para QUA não mexe na flag.** Enviar para qualidade é um teste; marcar a conta como
enviada faria o processo automático saltá-la no envio real para produção. Só o envio para
PRD escreve `Y` (aceite) ou `E` (recusado).

**PUT primeiro, POST depois.** O portal não tem "criar ou atualizar": a existência do registo
descobre-se pela resposta ao PUT. Ver um PUT falhado seguido de um POST com sucesso é o
caminho normal de uma criação, não um erro.

**O token vai em `multipart/form-data`.** É o formato que o endpoint OAuth do portal aceita;
com `form-urlencoded` a resposta é 401. Igual ao `AlwaysMultipartFormData` do original.

**A conta vai sempre com a subconta.** Escolher uma subconta no ecrã de envio não envia só
essa: envia a conta e essa subconta. O portal recusa uma subconta cuja conta não conheça.

**O envio lê as subcontas por uma consulta com joins, não pela tabela.** `CONTAS_FROMTO`
traduz o serviço, e `ACCOUNT_ALTERNATE_CONFIG` sobrepõe peso e tipo de destino. A consulta é
copiada do `ContasPortal_NET` de propósito: venha de onde vier, o portal tem de receber o
mesmo payload. É por isso que o campo Peso do ecrã pode não ser o peso enviado.

**O SQL é construído a partir do atributo `[Coluna]`.** A subconta tem mais de sessenta
colunas com nomes que nenhuma convenção automática adivinha (`BICSCCC2`,
`SE_INFLIGHT_LOJAS_LIM_P`, e `INFIGHT_DATA` — escrito mesmo assim na tabela). Declarar a
correspondência uma vez evita repetir sessenta nomes no SELECT, no INSERT e no UPDATE.

**As colunas são `CHAR` de tamanho fixo:** tudo o que é lido leva `TrimEnd`, senão os brancos
à direita chegam ao ecrã e ao portal.

**IDT no INSERT:** insere-se sem IDT e, se o Oracle devolver `ORA-01400`, repete-se com
`NVL(MAX(IDT),0)+1`. As tabelas vêm do AS400 e nem sempre têm sequência ou trigger do lado
do Oracle — o comportamento certo descobre-se na primeira inserção, não no código.

**Campo vazio grava NULL, não string vazia.** A diferença conta no envio: o nulo é
substituído pelos defaults que a API exige (`"ND"`, `"0"`, `"X"`), a string vazia não.

## Configuração

`appsettings.json`:

- `ConnectionStrings:Wsdpd` — Oracle, utilizador `CHRONO_WEB` (é o que o `ContasPortal_NET`
  usa para ler o WSDPD).
- `ContasPortal:Prd` / `ContasPortal:Qua` — endpoints OAuth, de contas e de subcontas, e as
  credenciais de cada ambiente.

## Estado

Verificado contra a base real: a listagem, a procura por nome, a ficha de uma conta e as suas
subcontas. **Por verificar**: gravar, e a consulta de envio com joins — o envio só se testa
mandando mesmo, e por isso faz-se primeiro para **QUA**.

A ligação a `10.2.3.30:1521` é intermitente a partir de fora da rede DPD: chegou a falhar com
ORA-12170 (também para o EF e o OraConsole) e a responder minutos depois sem alteração nenhuma.
Um timeout aqui não é sinal de erro no código.

A contagem de subcontas na listagem fica **fora** do `ROWNUM` de propósito: dentro da consulta
ordenada era calculada para toda a `AS400_CONTAS` antes do corte, e a listagem sem filtro
passava do minuto.

Fica também de fora, por não ter sido pedido: o `ContasPortal_NET` sincroniza cada subconta
com a base MySQL do **DPD Go** (`dpdgo.accounts`) logo a seguir ao envio. Um envio feito por
aqui **não** faz essa sincronização.
