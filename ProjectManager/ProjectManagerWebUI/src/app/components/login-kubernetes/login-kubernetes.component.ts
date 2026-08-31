import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { KubernetesAuthService } from '../../services/kubernetes-auth.service';

@Component({
  selector: 'app-login-kubernetes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login-kubernetes.component.html',
  styleUrls: ['./login-kubernetes.component.css'],
})
export class LoginKubernetesComponent implements OnInit {
  form = { email: '', password: '' };
  loading = false;
  submitted = false;
  erro = '';
  aviso = '';

  constructor(
    private auth: KubernetesAuthService,
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    if (this.route.snapshot.queryParams['sessao'] === 'expirada') {
      this.aviso = 'A sua sessão expirou. Entre novamente para continuar.';
    }
  }

  onSubmit(): void {
    this.submitted = true;
    this.erro = '';
    this.aviso = '';

    if (!this.form.email || !this.form.password) return;

    this.loading = true;
    this.auth.login(this.form.email, this.form.password).subscribe({
      next: (resposta) => {
        this.loading = false;

        if (resposta.success) {
          this.router.navigate(['/kubernetes']);
        } else {
          this.erro = resposta.message || 'Falha no login';
        }

        // Sem isto o ecrã fica no estado "a entrar" depois de um erro: o subscribe
        // corre fora da deteção de alterações, como nos outros logins do repositório.
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.loading = false;
        this.erro = err?.error?.message || 'Falha no login. Verifique as suas credenciais.';
        this.cdr.detectChanges();
      },
    });
  }

  voltar(): void {
    this.router.navigate(['/portal']);
  }
}
