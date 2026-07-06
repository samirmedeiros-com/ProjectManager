import { Injectable } from '@angular/core';
import { Router, CanActivate } from '@angular/router';
import { OraConsoleAuthService } from '../services/oraconsole-auth.service';

@Injectable({ providedIn: 'root' })
export class OraConsoleAuthGuard implements CanActivate {
  constructor(private router: Router, private oraAuthService: OraConsoleAuthService) {}

  canActivate(): boolean {
    if (this.oraAuthService.isAuthenticated()) {
      return true;
    }
    this.router.navigate(['/login-oraconsole']);
    return false;
  }
}
