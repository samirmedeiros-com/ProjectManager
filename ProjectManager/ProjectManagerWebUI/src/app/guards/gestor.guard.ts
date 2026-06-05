import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class GestorGuard implements CanActivate {
  constructor(private router: Router, private authService: AuthService) { }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
    const currentUser = this.authService.currentUserValue;

    if (currentUser && (currentUser.role === 'Admin' || currentUser.role === 'Gestor')) {
      return true;
    }

    // Se não tem permissão, redireciona para dashboard
    this.router.navigate(['/dashboard']);
    return false;
  }
}
