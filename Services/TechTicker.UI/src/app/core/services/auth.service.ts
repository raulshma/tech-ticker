import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
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
  token: string;
  user: User;
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
    return this.apiService.post<ApiResponse<AuthResponse>>('/users/login', credentials)
      .pipe(
        tap(response => {
          if (response.success && response.data) {
            this.setToken(response.data.token);
            this.currentUserSubject.next(response.data.user);
            this.isAuthenticatedSubject.next(true);
          }
        })
      );
  }

  register(userData: RegisterRequest): Observable<ApiResponse<AuthResponse>> {
    return this.apiService.post<ApiResponse<AuthResponse>>('/users/register', userData)
      .pipe(
        tap(response => {
          if (response.success && response.data) {
            this.setToken(response.data.token);
            this.currentUserSubject.next(response.data.user);
            this.isAuthenticatedSubject.next(true);
          }
        })
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
    return this.apiService.put<ApiResponse<any>>('/users/me/password', passwordData);
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
