import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> | Promise<boolean> | boolean {
    
    // Check if user is authenticated first
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    // Get required roles from route data
    const requiredRoles = route.data['roles'] as string[];
    const requireAll = route.data['requireAll'] as boolean || false;
    const redirectTo = route.data['redirectTo'] as string || '/dashboard';

    // If no roles specified, allow access (just authentication required)
    if (!requiredRoles || requiredRoles.length === 0) {
      return true;
    }

    // Check if user has required roles
    const hasAccess = this.authService.canAccess(requiredRoles, requireAll);

    if (!hasAccess) {
      // User doesn't have required roles, redirect to specified page or dashboard
      this.router.navigate([redirectTo]);
      return false;
    }

    return true;
  }
}
