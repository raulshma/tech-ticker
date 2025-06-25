import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthorizationInterceptor implements HttpInterceptor {
  constructor(
    private router: Router,
    private snackBar: MatSnackBar,
    private authService: AuthService
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          // Unauthorized - token expired or invalid
          this.handleUnauthorized();
        } else if (error.status === 403) {
          // Forbidden - user doesn't have required permissions
          this.handleForbidden();
        }
        
        return throwError(() => error);
      })
    );
  }

  private handleUnauthorized(): void {
    // Clear user session and redirect to login
    this.authService.logout();
    this.router.navigate(['/login']);
    this.snackBar.open('Your session has expired. Please log in again.', 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }

  private handleForbidden(): void {
    // User is authenticated but doesn't have permission
    this.snackBar.open('You do not have permission to access this resource.', 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
    
    // Redirect to dashboard or previous page
    this.router.navigate(['/dashboard']);
  }
}
