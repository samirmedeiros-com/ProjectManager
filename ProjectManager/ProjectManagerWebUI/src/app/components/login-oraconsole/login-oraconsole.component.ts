import { Component, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OraConsoleAuthService } from '../../services/oraconsole-auth.service';
import { OraConsoleLoginRequest } from '../../models/oraconsole.model';

@Component({
  selector: 'app-login-oraconsole',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login-oraconsole.component.html',
  styleUrls: ['./login-oraconsole.component.css']
})
export class LoginOraConsoleComponent {
  form: OraConsoleLoginRequest = { username: '', password: '' };
  loading = false;
  submitted = false;
  error = '';

  constructor(
    private oraAuthService: OraConsoleAuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  onSubmit() {
    this.submitted = true;
    this.error = '';
    if (!this.form.username || !this.form.password) return;

    this.loading = true;
    this.oraAuthService.login(this.form).subscribe({
      next: (response) => {
        if (response.success) {
          this.router.navigate(['/oraconsole/workbench']);
        } else {
          this.error = response.message || 'Falha no login';
        }
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.error = err?.error?.message || 'Falha no login. Verifique as credenciais Oracle.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  goBack() {
    this.router.navigate(['/portal']);
  }
}
