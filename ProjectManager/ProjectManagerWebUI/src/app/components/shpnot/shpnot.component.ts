import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AbaShpNot,
  CampoShpNot,
  NoShpNot,
  ShpNotDetalhe,
  ShpNotResumo,
  ShpNotService,
} from '../../services/shpnot.service';

@Component({
  selector: 'app-shpnot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './shpnot.component.html',
  styleUrls: ['./shpnot.component.scss'],
})
export class ShpNotComponent {
  // ------------------------------------------------------------- filtros
  /**
   * Vazia por omissão: a listagem abre nos últimos a entrar, que saem pelo índice do IDT.
   * Escolher um dia obriga a varrer a tabela — o ecrã avisa quando isso acontece.
   */
  data = '';
  /**
   * Um número só. Tanto serve a guia-mãe como a etiqueta de um volume: procura-se pelos dois
   * e devolve-se sempre o envio inteiro, que é o que interessa a quem tem um volume na mão.
   */
  numero = '';
  estado = '';
  respserv = '';
  /** Vazio traz os dois lados; senão só o que recebemos ou só o que enviamos. */
  sentido = '';

  readonly tamanhosPagina = [10, 50, 100];

  // ------------------------------------------------------------- estado
  linhas = signal<ShpNotResumo[]>([]);
  haMais = signal(false);
  pagina = signal(1);
  tamanho = signal(10);

  detalhe = signal<ShpNotDetalhe | null>(null);
  abaAtiva = signal('envio');
  /**
   * As linhas abertas das coleções, por chave.
   *
   * <p>É um conjunto e não uma chave só porque as coleções estão dentro umas das outras: as
   * notificações e as matérias perigosas vivem <b>dentro</b> de um volume. Com uma chave
   * única, abrir uma notificação fechava o volume que a continha — e a notificação
   * desaparecia no mesmo clique que a abria.</p>
   */
  linhasAbertas = signal<Set<string>>(new Set());

  aCarregar = signal(false);
  aCarregarDetalhe = signal(false);
  erro = signal('');
  procurou = signal(false);

  constructor(private servico: ShpNotService) {}

  // ------------------------------------------------------------ derivados

  abaCorrente = computed<AbaShpNot | null>(
    () => this.detalhe()?.abas.find((a) => a.chave === this.abaAtiva()) ?? null,
  );

  /**
   * O hint de um campo. Diz as três identidades do mesmo dado — como chegou no JSON, onde
   * está guardado, e com que nome viaja para o AS400. Quem consulta um SHPNOT está quase
   * sempre a comparar uma destas três vistas com outra.
   */
  hint(campo: CampoShpNot): string {
    const partes = [
      campo.json ? `JSON: ${campo.json}` : 'Campo de controlo (não vem no JSON)',
      `Tabela: ${campo.tabela}.${campo.coluna}`,
    ];

    // Num envio recebido o alias é o que ainda não se vê; num envio nosso o alias é a própria
    // coluna, e o que falta é onde o mesmo campo fica guardado quando somos nós a receber.
    if (campo.equivalente) partes.push(`Na receção: ${campo.equivalente}`);
    else partes.push(campo.alias ? `View: ${campo.alias}` : 'Não segue na VW_SHPNOT_AS400');

    return partes.join('\n');
  }

  /**
   * As listas guardadas como texto JSON mostram-se separadas por vírgulas; um `["a","b"]` no
   * meio de campos normais lê-se como lixo. Se o texto não for JSON válido, fica como está.
   */
  valor(campo: CampoShpNot): string {
    if (campo.valor === null || campo.valor === '') return '—';
    if (!campo.lista) return campo.valor;

    try {
      const lista = JSON.parse(campo.valor);
      return Array.isArray(lista) ? (lista.join(', ') || '—') : campo.valor;
    } catch {
      return campo.valor;
    }
  }

  /** Os blocos vazios ficam no fim: existem, mas não têm nada para ler. */
  ordenados(blocos: NoShpNot[]): NoShpNot[] {
    return [...blocos].sort((a, b) => Number(a.vazio) - Number(b.vazio));
  }

  // ------------------------------------------------------------ pesquisa

  procurar(): void {
    this.aCarregar.set(true);
    this.erro.set('');

    this.servico
      .procurar({
        // Uma pesquisa por MPS ID ou por volume não leva dia — e pedi-lo seria pior: quem
        // tem o número de um envio às mãos raramente sabe o dia em que ele entrou.
        data: this.numero.trim() ? undefined : this.data || undefined,
        mpsid: this.numero,
        estado: this.estado,
        respserv: this.respserv,
        sentido: this.sentido,
        pagina: this.pagina(),
        tamanho: this.tamanho(),
      })
      .subscribe({
        next: (p) => {
          this.linhas.set(p.itens);
          this.haMais.set(p.haMais);
          this.aCarregar.set(false);
          this.procurou.set(true);
        },
        error: (e) => {
          this.erro.set(this.porque(e, 'procurar SHPNOTs'));
          this.linhas.set([]);
          this.haMais.set(false);
          this.aCarregar.set(false);
          this.procurou.set(true);
        },
      });
  }

  limpar(): void {
    this.numero = '';
    this.sentido = '';
    this.estado = '';
    this.respserv = '';
    this.data = '';
    this.linhas.set([]);
    this.haMais.set(false);
    this.procurou.set(false);
    this.erro.set('');
    this.pagina.set(1);
  }

  irPara(p: number): void {
    if (p < 1 || (p > this.pagina() && !this.haMais())) return;
    this.pagina.set(p);
    this.procurar();
  }

  mudarTamanho(t: number): void {
    this.tamanho.set(Number(t));
    this.pagina.set(1);
    this.procurar();
  }

  // ------------------------------------------------------------ detalhe

  abrir(linha: ShpNotResumo): void {
    this.aCarregarDetalhe.set(true);
    this.erro.set('');
    this.linhasAbertas.set(new Set());

    this.servico.obter(linha.idt, linha.sentido).subscribe({
      next: (d) => {
        this.detalhe.set(d);
        this.abaAtiva.set(d.abas[0]?.chave ?? 'envio');
        this.aCarregarDetalhe.set(false);
      },
      error: (e) => {
        this.erro.set(this.porque(e, 'abrir o SHPNOT'));
        this.aCarregarDetalhe.set(false);
      },
    });
  }

  fechar(): void {
    this.detalhe.set(null);
  }

  alternarLinha(id: string): void {
    const abertas = new Set(this.linhasAbertas());
    if (!abertas.delete(id)) abertas.add(id);
    this.linhasAbertas.set(abertas);
  }

  linhaAberta(id: string): boolean {
    return this.linhasAbertas().has(id);
  }

  /**
   * O estado em palavras. A mesma letra quer dizer coisas diferentes nos dois sentidos: num
   * envio recebido, Y é "já foi integrado no AS400"; num envio nosso, Y é "já foi entregue
   * ao Geopost". Escrever "No AS400" numa linha de saída seria dizer o contrário do que é.
   */
  rotuloEstado(linha: { sentido: string; estado: string }): string {
    const saida = linha.sentido === 'saida';
    switch (linha.estado) {
      case 'enviado': return saida ? 'No Geopost' : 'No AS400';
      case 'erro': return 'Com erro';
      default: return saida ? 'Por enviar' : 'Por integrar';
    }
  }

  /**
   * Diz o que correu mal em vez de "não foi possível". Uma mensagem sem motivo manda quem a
   * lê adivinhar entre sessão expirada, base em baixo e erro de código — que é exactamente o
   * que aconteceu na primeira versão deste ecrã.
   */
  private porque(e: any, oQue: string): string {
    if (e?.status === 401) {
      return 'A sessão terminou. Volte a entrar na Gestão de Dados e tente de novo.';
    }
    if (e?.status === 0) {
      return `Não foi possível falar com o servidor ao ${oQue}. Pode ser rede ou a aplicação ` +
        'estar a reiniciar — tente daqui a pouco.';
    }

    const detalhe = typeof e?.error === 'string' ? e.error : e?.message;
    return `Não foi possível ${oQue}` +
      (e?.status ? ` (${e.status})` : '') +
      (detalhe ? `: ${detalhe}` : '.');
  }

  private hojeIso(): string {
    const hoje = new Date();
    const mes = `${hoje.getMonth() + 1}`.padStart(2, '0');
    const dia = `${hoje.getDate()}`.padStart(2, '0');
    return `${hoje.getFullYear()}-${mes}-${dia}`;
  }
}
