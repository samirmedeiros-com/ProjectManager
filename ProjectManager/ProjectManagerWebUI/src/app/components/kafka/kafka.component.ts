import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { Conector, KafkaService, ResumoKafka, TaskConector } from '../../services/kafka.service';

@Component({
  selector: 'app-kafka',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './kafka.component.html',
  styleUrls: ['./kafka.component.scss'],
})
export class KafkaComponent implements OnInit, OnDestroy {
  resumo = signal<ResumoKafka | null>(null);
  detalhe = signal<Conector | null>(null);

  aCarregar = signal(false);
  aReiniciar = signal('');
  erro = signal('');
  aviso = signal('');
  atualizadoEm = signal<Date | null>(null);

  /**
   * Relê sozinho de 30 em 30 segundos. O Kafka Connect não avisa ninguém quando uma task cai,
   * e um painel de estado que só muda quando alguém carrega em recarregar é um painel que
   * mostra o passado.
   */
  private temporizador?: ReturnType<typeof setInterval>;

  constructor(private servico: KafkaService) {}

  ngOnInit(): void {
    this.carregar();
    this.temporizador = setInterval(() => this.carregar(true), 30_000);
  }

  ngOnDestroy(): void {
    if (this.temporizador) clearInterval(this.temporizador);
  }

  /** `silencioso` para a releitura automática não pôr o ecrã em "a carregar" a cada 30 s. */
  carregar(silencioso = false): void {
    if (!silencioso) this.aCarregar.set(true);
    this.erro.set('');

    this.servico.conectores().subscribe({
      next: (r) => {
        this.resumo.set(r);
        this.atualizadoEm.set(new Date());
        this.aCarregar.set(false);
      },
      error: (e) => {
        this.aCarregar.set(false);
        this.erro.set(this.mensagem(e));
      },
    });
  }

  abrirDetalhe(c: Conector): void {
    // Relê do servidor: a listagem não traz o traço de erro das tasks, e é isso que interessa aqui.
    this.servico.conector(c.nome).subscribe({
      next: (d) => this.detalhe.set(d),
      error: (e) => this.erro.set(this.mensagem(e)),
    });
  }

  fecharDetalhe(): void {
    this.detalhe.set(null);
  }

  reiniciar(conector: string, task: TaskConector): void {
    // Reiniciar uma task falhada é o caso normal e não custa nada. Reiniciar uma que está a
    // correr corta a replicação por instantes — quem o faz deve sabê-lo antes, não depois.
    const aviso = task.emErro
      ? `Reiniciar a task ${task.id} do conector ${conector}?`
      : `A task ${task.id} de ${conector} está em ${task.estado}.\n\n` +
        'Reiniciá-la interrompe a replicação por instantes. Continuar?';

    if (!confirm(aviso)) return;

    const chave = `${conector}#${task.id}`;
    this.aReiniciar.set(chave);
    this.erro.set('');
    this.aviso.set('');

    this.servico.reiniciarTask(conector, task.id).subscribe({
      next: (r) => {
        this.aReiniciar.set('');

        if (!r.sucesso) {
          this.erro.set(r.mensagem || 'O reinício não foi aceite.');
          return;
        }

        this.aviso.set(`Task ${r.task} de ${r.conector} reiniciada. O estado novo aparece dentro de alguns segundos.`);
        this.fecharDetalhe();

        // O Connect responde antes de a task arrancar: reler já mostraria o estado antigo.
        setTimeout(() => this.carregar(true), 4000);
      },
      error: (e) => {
        this.aReiniciar.set('');
        this.erro.set(this.mensagem(e));
      },
    });
  }

  aReiniciarEsta(conector: string, task: TaskConector): boolean {
    return this.aReiniciar() === `${conector}#${task.id}`;
  }

  /** A classe do crachá de estado. Tudo o que não é RUNNING nem PAUSED conta como avaria. */
  classeEstado(estado: string): string {
    const e = (estado || '').toUpperCase();
    if (e === 'RUNNING') return 'cracha--ok';
    if (e === 'PAUSED') return 'cracha--pausa';
    return 'cracha--erro';
  }

  private mensagem(e: unknown): string {
    const err = e as { error?: unknown; message?: string };
    if (typeof err?.error === 'string' && err.error.trim()) return err.error;
    return err?.message || 'Não foi possível falar com o Kafka Connect.';
  }
}
