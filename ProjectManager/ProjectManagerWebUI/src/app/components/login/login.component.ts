import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LoginRequest } from '../../models/user.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  form: LoginRequest = { email: '', password: '' };
  loading = false;
  submitted = false;
  error = '';

  constructor(private authService: AuthService, private router: Router) { }

  onSubmit() {
    this.submitted = true;
    this.error = '';

    if (!this.form.email || !this.form.password) {
      return;
    }

    this.loading = true;
    this.authService.login(this.form).subscribe(
      (response) => {
        if (response.success) {
          this.router.navigate(['/dashboard']);
        } else {
          this.error = response.message || 'Falha no login';
        }
        this.loading = false;
      },
      (error) => {
        this.error = 'Falha no login. Por favor, verifique suas credenciais.';
        this.loading = false;
      }
    );
  }
}
