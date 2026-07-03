import { Injectable } from '@angular/core';
import { Router, CanActivate } from '@angular/router';
import { SeurAuthService } from '../services/seur-auth.service';

@Injectable({ providedIn: 'root' })
export class SeurAuthGuard implements CanActivate {
  constructor(private router: Router, private seurAuthService: SeurAuthService) {}

  canActivate(): boolean {
    if (this.seurAuthService.isAuthenticated()) {
      return true;
    }
    this.router.navigate(['/login-seur']);
    return false;
  }
}
