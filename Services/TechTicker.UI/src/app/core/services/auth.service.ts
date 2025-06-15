import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap, map } from 'rxjs';
import { ApiService, ApiResponse } from './api.service';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface AuthResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  scope?: string;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  isActive: boolean;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  email: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);

  public currentUser$ = this.currentUserSubject.asObservable();
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor(
    private apiService: ApiService,
    private router: Router
  ) {
    this.initializeAuth();
  }

  private initializeAuth(): void {
    const token = this.getToken();
    if (token) {
      this.getCurrentUser().subscribe({
        next: (response) => {
          this.currentUserSubject.next(response.data);
          this.isAuthenticatedSubject.next(true);
        },
        error: () => {
          this.logout();
        }
      });
    }
  }

  login(credentials: LoginRequest): Observable<ApiResponse<AuthResponse>> {
    // Use OAuth2 password grant with OpenIddict
    const tokenRequest = {
      grant_type: 'password',
      username: credentials.email,
      password: credentials.password,
      client_id: 'TechTicker.Client',
      client_secret: 'TechTicker.Secret',
      scope: 'email profile roles'
    };

    return this.apiService.postFormEncoded<AuthResponse>('/auth/connect/token', tokenRequest)
      .pipe(
        tap(response => {
          if (response.access_token) {
            this.setToken(response.access_token);
            this.isAuthenticatedSubject.next(true);
            // Get user info after successful token exchange
            this.getCurrentUser().subscribe({
              next: (userResponse) => {
                this.currentUserSubject.next(userResponse.data);
              },
              error: () => {
                this.logout();
              }
            });
          }
        }),
        // Transform the OAuth2 response to match our ApiResponse format
        map(response => ({
          success: !!response.access_token,
          data: response,
          message: response.access_token ? 'Login successful' : 'Login failed'
        }))
      );
  }

  register(userData: RegisterRequest): Observable<ApiResponse<AuthResponse>> {
    // First register the user, then login
    return this.apiService.post<any>('/users/register', userData)
      .pipe(
        tap(response => {
          if (response.success) {
            // After successful registration, automatically login
            this.login({
              email: userData.email,
              password: userData.password
            }).subscribe();
          }
        }),
        // Transform to match our expected format
        map(response => ({
          success: response.success || false,
          data: { access_token: '', token_type: 'Bearer', expires_in: 0 } as AuthResponse,
          message: response.message || 'Registration completed'
        }))
      );
  }

  getCurrentUser(): Observable<ApiResponse<User>> {
    return this.apiService.get<ApiResponse<User>>('/users/me');
  }

  updateProfile(profileData: UpdateProfileRequest): Observable<ApiResponse<User>> {
    return this.apiService.put<ApiResponse<User>>('/users/me', profileData)
      .pipe(
        tap(response => {
          if (response.success && response.data) {
            this.currentUserSubject.next(response.data);
          }
        })
      );
  }

  changePassword(passwordData: ChangePasswordRequest): Observable<ApiResponse<any>> {
    return this.apiService.post<ApiResponse<any>>('/users/me/change-password', passwordData);
  }

  logout(): void {
    this.removeToken();
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    this.router.navigate(['/auth/login']);
  }

  private setToken(token: string): void {
    localStorage.setItem('auth_token', token);
  }

  private getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  private removeToken(): void {
    localStorage.removeItem('auth_token');
  }

  get currentUser(): User | null {
    return this.currentUserSubject.value;
  }

  get isAuthenticated(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  hasRole(role: string): boolean {
    const user = this.currentUser;
    return user ? user.roles.includes(role) : false;
  }

  hasAnyRole(roles: string[]): boolean {
    const user = this.currentUser;
    return user ? roles.some(role => user.roles.includes(role)) : false;
  }
}
