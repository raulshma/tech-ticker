import { Injectable } from '@angular/core';
import { CanActivate, CanActivateChild, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable, map, take } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AdminGuard implements CanActivate, CanActivateChild {

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    return this.checkAdminRole(state.url);
  }

  canActivateChild(
    childRoute: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    return this.checkAdminRole(state.url);
  }

  private checkAdminRole(redirectUrl: string): Observable<boolean> {
    return this.authService.isAuthenticated$.pipe(
      take(1),
      map(isAuthenticated => {
        if (!isAuthenticated) {
          this.router.navigate(['/auth/login'], {
            queryParams: { returnUrl: redirectUrl }
          });
          return false;
        }

        if (this.authService.hasRole('Admin')) {
          return true;
        } else {
          this.router.navigate(['/dashboard'], {
            queryParams: { error: 'access_denied' }
          });
          return false;
        }
      })
    );
  }
}
