import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TracePushComponent } from '../tracepush/tracepush.component';
import { Router } from '@angular/router';
import { SeurAuthService } from '../../services/seur-auth.service';
import {
  Conta,
  ContaResumo,
  ContasEstatisticas,
  ContasService,
  EnvioLog,
  EnvioLogLinha,
  ResultadoEnvio,
  SubConta,
} from '../../services/contas.service';

/** Em que ecrã está a aplicação. A lista e o detalhe são telas, não painéis lado a lado. */
type Vista = 'lista' | 'detalhe' | 'registo';

/**
 * Entrada do menu de navegação da Gestão de Dados. Por agora só "Contas", mas o menu existe
 * já como menu (e não como um único botão) porque vão entrar mais módulos ao lado deste.
 */
interface ItemMenu {
  chave: string;
  etiqueta: string;
  ativo: boolean;
}

/** Campos de uma subconta agrupados como se lêem no ecrã, não como estão na tabela. */
interface GrupoCampos {
  titulo: string;
  campos: { chave: keyof SubConta; etiqueta: string; ajuda?: string }[];
}

@Component({
  selector: 'app-contas',
  standalone: true,
  imports: [CommonModule, FormsModule, TracePushComponent],
  templateUrl: './contas.component.html',
  styleUrls: ['./contas.component.scss'],
})
export class ContasComponent implements OnInit {
  vista = signal<Vista>('lista');

  /** Módulos da Gestão de Dados. "Contas" é o único ativo; os outros ficam para depois. */
  readonly menu: ItemMenu[] = [
    { chave: 'contas', etiqueta: 'Contas', ativo: true },
    { chave: 'tracepush', etiqueta: 'Trace Push', ativo: true },
  ];
  moduloAtivo = signal('contas');

  procura = '';
  filtroFlag = '';

  estatisticas = signal<ContasEstatisticas | null>(null);

  readonly tamanhosPagina = [10, 50, 100];

  // Paginação das contas (server-side)
  contas = signal<ContaResumo[]>([]);
  contasTotal = signal(0);
  contasPagina = signal(1);
  contasTamanho = signal(10);

  // Paginação das subcontas (client-side: já vêm todas ao abrir a conta)
  subContas = signal<SubConta[]>([]);
  subPagina = signal(1);
  subTamanho = signal(10);
  contaAberta = signal<Conta | null>(null);
  subContaAberta = signal<SubConta | null>(null);

  aCarregar = signal(false);
  aGravar = signal(false);
  aEnviar = signal(false);
  erro = signal('');
  aviso = signal('');

  /** Envio: o ambiente é sempre escolhido à mão, nunca herdado do envio anterior. */
  ambiente = signal<'PRD' | 'QUA' | ''>('');
  envioSubConta = signal<string>('');
  resultadoEnvio = signal<ResultadoEnvio | null>(null);
  painelEnvio = signal(false);

  // Registo de envios (ecrã completo)
  logs = signal<EnvioLog[]>([]);
  logExpandido = signal<number | null>(null);
  logLinhas = signal<EnvioLogLinha[]>([]);
  aCarregarLogs = signal(false);

  // Registo de envios da conta aberta (no detalhe)
  logsConta = signal<EnvioLog[]>([]);
  logContaExpandido = signal<number | null>(null);
  logContaLinhas = signal<EnvioLogLinha[]>([]);

  /**
   * A subconta tem mais de sessenta colunas: mostradas em bloco, o ecrã fica ilegível.
   * A ordem aqui é a de quem trata da conta — identificação primeiro, contactos a seguir,
   * e as opções de serviço no fim.
   */
  readonly grupos: GrupoCampos[] = [
    {
      titulo: 'Identificação',
      campos: [
        { chave: 'ement', etiqueta: 'Conta (EMENT)' },
        { chave: 'emend', etiqueta: 'Subconta (EMEND)' },
        { chave: 'emdsc', etiqueta: 'Descrição' },
        { chave: 'active', etiqueta: 'Ativa', ajuda: 'O portal só aceita "true" como ativa' },
      ],
    },
    {
      titulo: 'Morada',
      campos: [
        { chave: 'emmor', etiqueta: 'Morada', ajuda: 'Cortada aos 64 caracteres no envio' },
        { chave: 'emloc', etiqueta: 'Localidade' },
        { chave: 'empos', etiqueta: 'Código postal' },
        { chave: 'empai', etiqueta: 'País' },
        { chave: 'emenf', etiqueta: 'Número' },
      ],
    },
    {
      titulo: 'Contactos',
      campos: [
        { chave: 'telefone', etiqueta: 'Telefone' },
        { chave: 'telemovel', etiqueta: 'Telemóvel' },
        { chave: 'email', etiqueta: 'Email' },
      ],
    },
    {
      titulo: 'Serviço',
      campos: [
        { chave: 'serviceRc', etiqueta: 'Serviço RC', ajuda: 'Traduzido por CONTAS_FROMTO no envio' },
        { chave: 'serviceB2c', etiqueta: 'Serviço B2C' },
        { chave: 'servFrio', etiqueta: 'Serviço frio' },
        { chave: 'serviceFrio', etiqueta: 'Service frio (código)' },
        { chave: 'fresh', etiqueta: 'Fresh (minutos)' },
        { chave: 'descServico', etiqueta: 'Descrição do serviço' },
        { chave: 'textoServico', etiqueta: 'Texto do serviço' },
        { chave: 'emtft', etiqueta: 'EMTFT' },
        { chave: 'emexp', etiqueta: 'EMEXP' },
        { chave: 'emtra', etiqueta: 'EMTRA' },
        { chave: 'bicact', etiqueta: 'BICACT' },
      ],
    },
    {
      titulo: 'Faturação (BICS)',
      campos: [
        { chave: 'bicsci', etiqueta: 'BICS CI' },
        { chave: 'bicsnif', etiqueta: 'NIF' },
        { chave: 'bicsccc', etiqueta: 'CCC' },
        { chave: 'bicsccc2', etiqueta: 'CCC 2' },
        { chave: 'bicspraca', etiqueta: 'Praça' },
        { chave: 'bicsusr2', etiqueta: 'Utilizador 2' },
        { chave: 'bicsserv', etiqueta: 'Serviço' },
        { chave: 'bicsprod', etiqueta: 'Produto' },
        { chave: 'bicvcli', etiqueta: 'Cliente (BICVCLI)' },
      ],
    },
    {
      titulo: 'Opções de entrega',
      campos: [
        { chave: 'predict', etiqueta: 'Predict (Y/N)' },
        { chave: 'grassinada', etiqueta: 'Guia assinada (Y/N)' },
        { chave: 'multiparcela', etiqueta: 'Multiparcela (Y/N)' },
        { chave: 'cod', etiqueta: 'Cobrança (Y/N)' },
        { chave: 'servicoRcComCod', etiqueta: 'Serviço RC com cobrança' },
        { chave: 'swap', etiqueta: 'Swap (Y/N)' },
        { chave: 'pickupExpeditor', etiqueta: 'Recolha no expedidor (Y/N)' },
        { chave: 'pickupDestinatario', etiqueta: 'Recolha no destinatário (Y/N)' },
        { chave: 'moradaDestinatario', etiqueta: 'Morada do destinatário (Y/N)' },
        { chave: 'tipoDestino', etiqueta: 'Tipo de destino' },
        { chave: 'reenAutoLoja', etiqueta: 'Reencaminhamento automático loja (Y/N)' },
        { chave: 'lojaAMostrar', etiqueta: 'Loja a mostrar' },
        { chave: 'lockreppic', etiqueta: 'LOCKREPPIC' },
        { chave: 'recprtdpdc', etiqueta: 'RECPRTDPDC' },
      ],
    },
    {
      titulo: 'Pesos e medidas',
      campos: [
        { chave: 'limitePeso', etiqueta: 'Limite de peso (Y/N)' },
        { chave: 'peso', etiqueta: 'Peso', ajuda: 'ACCOUNT_ALTERNATE_CONFIG sobrepõe-se a este valor no envio' },
        { chave: 'dimensoes', etiqueta: 'Dimensões' },
        { chave: 'medidas', etiqueta: 'Medidas' },
      ],
    },
    {
      titulo: 'Etiquetas',
      campos: [
        { chave: 'templateEtiqueta', etiqueta: 'Template' },
        { chave: 'textEtiquetaAuxiliar', etiqueta: 'Texto auxiliar' },
        { chave: 'templateEtiquetaComCod', etiqueta: 'Template com cobrança' },
        { chave: 'textEtiquetaComCod', etiqueta: 'Texto com cobrança' },
      ],
    },
    {
      titulo: 'Inflight',
      campos: [
        { chave: 'inflightLojas', etiqueta: 'Lojas' },
        { chave: 'seInflightLojasLimVol', etiqueta: 'Limite de volumes' },
        { chave: 'seInflightLojasLimP', etiqueta: 'Limite de peso' },
        { chave: 'infightData', etiqueta: 'Data (Y/N)', ajuda: 'A coluna chama-se mesmo INFIGHT_DATA' },
        { chave: 'inflightMorada', etiqueta: 'Morada (Y/N)' },
      ],
    },
  ];

  // Páginas totais de contas, para desativar o "seguinte" na última.
  contasTotalPaginas = computed(() =>
    Math.max(1, Math.ceil(this.contasTotal() / this.contasTamanho())));

  // A fatia de subcontas da página atual, calculada sobre a lista já carregada.
  subContasPagina = computed(() => {
    const inicio = (this.subPagina() - 1) * this.subTamanho();
    return this.subContas().slice(inicio, inicio + this.subTamanho());
  });
  subTotalPaginas = computed(() =>
    Math.max(1, Math.ceil(this.subContas().length / this.subTamanho())));

  constructor(
    private servico: ContasService,
    private seurAuth: SeurAuthService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.carregarContas();
    this.carregarEstatisticas();
  }

  selecionarModulo(chave: string): void {
    if (!this.menu.find((m) => m.chave === chave)?.ativo) return;
    this.moduloAtivo.set(chave);
    this.voltar();
  }

  /** Recarrega os números do dashboard. Chamado ao entrar, ao gravar e depois de um envio. */
  carregarEstatisticas(): void {
    this.servico.estatisticas().subscribe({
      next: (e) => this.estatisticas.set(e),
      // O dashboard é informativo: se falhar, não estraga a lista — fica só sem números.
      error: () => this.estatisticas.set(null),
    });
  }

  get utilizador(): string {
    return this.seurAuth.currentUserValue?.fullName ?? this.seurAuth.currentUserValue?.email ?? '';
  }

  sair(): void {
    this.seurAuth.logout();
    this.router.navigate(['/portal']);
  }

  voltarAoPortal(): void {
    this.router.navigate(['/portal']);
  }

  // ------------------------------------------------------------------ contas

  carregarContas(): void {
    this.aCarregar.set(true);
    this.erro.set('');

    this.servico.listar(this.procura, this.filtroFlag, this.contasPagina(), this.contasTamanho()).subscribe({
      next: (pagina) => {
        this.contas.set(pagina.itens);
        this.contasTotal.set(pagina.total);
        // Se uma procura reduziu o total abaixo da página onde estávamos, recua para a última.
        const ultima = this.contasTotalPaginas();
        if (this.contasPagina() > ultima) {
          this.contasPagina.set(ultima);
          this.carregarContas();
          return;
        }
        this.aCarregar.set(false);
      },
      error: (e) => {
        this.erro.set(this.mensagem(e, 'Não foi possível carregar as contas.'));
        this.aCarregar.set(false);
      },
    });
  }

  /** Procurar/filtrar recomeça sempre na primeira página. */
  procurar(): void {
    this.contasPagina.set(1);
    this.carregarContas();
  }

  irParaPaginaContas(pagina: number): void {
    if (pagina < 1 || pagina > this.contasTotalPaginas() || pagina === this.contasPagina()) return;
    this.contasPagina.set(pagina);
    this.carregarContas();
  }

  mudarTamanhoContas(tamanho: number): void {
    this.contasTamanho.set(Number(tamanho));
    this.contasPagina.set(1);
    this.carregarContas();
  }

  // Subcontas: paginação sobre a lista já carregada, sem ir ao servidor.
  irParaPaginaSub(pagina: number): void {
    if (pagina < 1 || pagina > this.subTotalPaginas()) return;
    this.subPagina.set(pagina);
  }

  mudarTamanhoSub(tamanho: number): void {
    this.subTamanho.set(Number(tamanho));
    this.subPagina.set(1);
  }

  abrirConta(eeent: string): void {
    this.erro.set('');
    this.aviso.set('');
    this.resultadoEnvio.set(null);
    this.painelEnvio.set(false);

    this.servico.obter(eeent).subscribe({
      next: (conta) => {
        this.contaAberta.set(conta);
        this.vista.set('detalhe');
        this.carregarSubContas(eeent);
        this.carregarLogsConta(eeent);
      },
      error: (e) => this.erro.set(this.mensagem(e, `Não foi possível abrir a conta ${eeent}.`)),
    });
  }

  novaConta(): void {
    this.erro.set('');
    this.aviso.set('');
    this.subContas.set([]);
    this.resultadoEnvio.set(null);
    this.painelEnvio.set(false);
    this.contaAberta.set({
      eeent: '', eenom: '', eenom2: '', eectb: '', eecae: '',
      ftsit: '', ftsgm: '', ftzon: '', ftven: '', flagportal: 'N',
    });
    this.vista.set('detalhe');
  }

  /** Volta da tela de detalhe (ou do registo) para a lista, sem perder a procura feita. */
  voltar(): void {
    this.contaAberta.set(null);
    this.subContas.set([]);
    this.resultadoEnvio.set(null);
    this.painelEnvio.set(false);
    this.erro.set('');
    this.aviso.set('');
    this.vista.set('lista');
    this.carregarContas();
  }

  gravarConta(): void {
    const conta = this.contaAberta();
    if (!conta) return;

    if (!conta.eeent?.trim()) {
      this.erro.set('O número da conta é obrigatório.');
      return;
    }

    this.aGravar.set(true);
    this.erro.set('');

    this.servico.gravar(conta).subscribe({
      next: (gravada) => {
        this.contaAberta.set(gravada);
        this.aGravar.set(false);
        // "Por enviar" não é um detalhe: é o que faz a alteração chegar ao portal, aqui
        // ou pelo processo automático. Dizê-lo evita a pergunta seguinte.
        this.aviso.set(`Conta ${gravada.eeent} gravada e marcada como por enviar.`);
        this.carregarContas();
        this.carregarEstatisticas();
        this.carregarSubContas(gravada.eeent!);
      },
      error: (e) => {
        this.erro.set(this.mensagem(e, 'Não foi possível gravar a conta.'));
        this.aGravar.set(false);
      },
    });
  }

  // --------------------------------------------------------------- subcontas

  private carregarSubContas(eeent: string): void {
    this.subPagina.set(1);
    this.servico.subContas(eeent).subscribe({
      next: (lista) => this.subContas.set(lista),
      error: (e) => this.erro.set(this.mensagem(e, 'Não foi possível carregar as subcontas.')),
    });
  }

  abrirSubConta(sub: SubConta): void {
    this.erro.set('');
    // Cópia: sem ela, escrever no formulário altera a linha da lista mesmo que se cancele.
    this.subContaAberta.set({ ...sub });
  }

  novaSubConta(): void {
    const conta = this.contaAberta();
    if (!conta?.eeent) return;

    this.erro.set('');
    const vazia = {} as SubConta;
    for (const grupo of this.grupos)
      for (const campo of grupo.campos) (vazia as any)[campo.chave] = null;

    vazia.ement = conta.eeent;
    vazia.emend = '';
    vazia.active = 'true';
    this.subContaAberta.set(vazia);
  }

  fecharSubConta(): void {
    this.subContaAberta.set(null);
  }

  gravarSubConta(): void {
    const sub = this.subContaAberta();
    if (!sub) return;

    if (!sub.emend?.trim()) {
      this.erro.set('O número da subconta é obrigatório.');
      return;
    }

    this.aGravar.set(true);
    this.erro.set('');

    this.servico.gravarSubConta(sub).subscribe({
      next: (gravada) => {
        this.aGravar.set(false);
        this.subContaAberta.set(null);
        this.aviso.set(`Subconta ${gravada.ement}/${gravada.emend} gravada e marcada como por enviar.`);
        this.carregarSubContas(gravada.ement!);
        this.carregarEstatisticas();
      },
      error: (e) => {
        this.erro.set(this.mensagem(e, 'Não foi possível gravar a subconta.'));
        this.aGravar.set(false);
      },
    });
  }

  valor(sub: SubConta, chave: keyof SubConta): string {
    const v = sub[chave];
    return v === null || v === undefined ? '' : String(v);
  }

  escrever(chave: keyof SubConta, valor: string): void {
    const sub = this.subContaAberta();
    if (!sub) return;
    // Campo apagado grava NULL, não uma string vazia: a diferença conta no envio, onde
    // o nulo é substituído por um default ("ND", "0") e a string vazia não.
    (sub as any)[chave] = valor === '' ? null : valor;
    this.subContaAberta.set({ ...sub });
  }

  // ------------------------------------------------------------------- envio

  abrirEnvio(): void {
    this.resultadoEnvio.set(null);
    this.ambiente.set('');
    this.envioSubConta.set('');
    this.painelEnvio.set(true);
  }

  enviar(): void {
    const conta = this.contaAberta();
    const amb = this.ambiente();
    if (!conta?.eeent || !amb) return;

    this.aEnviar.set(true);
    this.erro.set('');
    this.aviso.set('');
    this.resultadoEnvio.set(null);

    this.servico.enviar({
      ambiente: amb,
      conta: conta.eeent,
      subConta: this.envioSubConta() || null,
    }).subscribe({
      next: (resultado) => {
        this.resultadoEnvio.set(resultado);
        this.aEnviar.set(false);
        this.carregarContas();
        this.carregarEstatisticas();
        this.carregarSubContas(conta.eeent!);
        this.carregarLogsConta(conta.eeent!);
      },
      error: (e) => {
        this.erro.set(this.mensagem(e, 'Não foi possível enviar ao portal.'));
        this.aEnviar.set(false);
      },
    });
  }

  // ------------------------------------------------------- registo de envios

  abrirRegisto(): void {
    this.erro.set('');
    this.logExpandido.set(null);
    this.logLinhas.set([]);
    this.vista.set('registo');
    this.carregarLogs();
  }

  carregarLogs(): void {
    this.aCarregarLogs.set(true);
    this.servico.logs(100).subscribe({
      next: (l) => { this.logs.set(l); this.aCarregarLogs.set(false); },
      error: (e) => {
        this.erro.set(this.mensagem(e, 'Não foi possível carregar o registo de envios.'));
        this.aCarregarLogs.set(false);
      },
    });
  }

  /** Abre (ou fecha) as respostas de conta/subconta de um envio registado. */
  alternarLinhas(log: EnvioLog): void {
    if (this.logExpandido() === log.id) {
      this.logExpandido.set(null);
      this.logLinhas.set([]);
      return;
    }

    this.logExpandido.set(log.id);
    this.logLinhas.set([]);
    this.servico.logLinhas(log.id).subscribe({
      next: (linhas) => this.logLinhas.set(linhas),
      error: (e) => this.erro.set(this.mensagem(e, 'Não foi possível abrir as respostas deste envio.')),
    });
  }

  // ------------------------------------------- registo de envios da conta aberta

  carregarLogsConta(eeent: string): void {
    this.logContaExpandido.set(null);
    this.logContaLinhas.set([]);
    this.servico.logs(50, eeent).subscribe({
      next: (l) => this.logsConta.set(l),
      // Informativo: se falhar, não estraga o detalhe da conta.
      error: () => this.logsConta.set([]),
    });
  }

  alternarLinhasConta(log: EnvioLog): void {
    if (this.logContaExpandido() === log.id) {
      this.logContaExpandido.set(null);
      this.logContaLinhas.set([]);
      return;
    }

    this.logContaExpandido.set(log.id);
    this.logContaLinhas.set([]);
    this.servico.logLinhas(log.id).subscribe({
      next: (linhas) => this.logContaLinhas.set(linhas),
      error: (e) => this.erro.set(this.mensagem(e, 'Não foi possível abrir as respostas deste envio.')),
    });
  }

  // ----------------------------------------------------------------- comuns

  estado(flag: string | null | undefined): string {
    switch ((flag ?? '').trim()) {
      case 'Y': return 'Enviada';
      case 'E': return 'Erro no envio';
      case 'N': return 'Por enviar';
      default: return '—';
    }
  }

  classeEstado(flag: string | null | undefined): string {
    switch ((flag ?? '').trim()) {
      case 'Y': return 'estado estado--ok';
      case 'E': return 'estado estado--erro';
      case 'N': return 'estado estado--pendente';
      default: return 'estado';
    }
  }

  private mensagem(erro: any, alternativa: string): string {
    if (typeof erro?.error === 'string' && erro.error.trim()) return erro.error;
    return erro?.message ? `${alternativa} (${erro.message})` : alternativa;
  }
}
