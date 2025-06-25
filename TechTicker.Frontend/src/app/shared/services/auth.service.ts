import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, catchError, throwError, map } from 'rxjs';
import {
  TechTickerApiClient,
  LoginUserDto,
  RegisterUserDto,
  LoginResponseDto,
  UserDto,
  LoginResponseDtoApiResponse,
  UserDtoApiResponse
} from '../api/api-client';

export interface CurrentUser {
  userId: string;
  email: string;
  firstName?: string;
  lastName?: string;
  roles: string[];
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'techticker_token';
  private readonly USER_KEY = 'techticker_user';

  private currentUserSubject = new BehaviorSubject<CurrentUser | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private apiClient: TechTickerApiClient
  ) {
    // Load user from localStorage on service initialization
    this.loadUserFromStorage();
  }

  private loadUserFromStorage(): void {
    const token = localStorage.getItem(this.TOKEN_KEY);
    const userJson = localStorage.getItem(this.USER_KEY);

    if (token && userJson) {
      try {
        const user = JSON.parse(userJson);
        this.currentUserSubject.next(user);
      } catch (error) {
        console.error('Error parsing stored user data:', error);
        this.logout();
      }
    }
  }

  private setCurrentUser(loginResponse: LoginResponseDto): void {
    if (!loginResponse.token || !loginResponse.userId) {
      throw new Error('Invalid login response: missing token or userId');
    }

    localStorage.setItem(this.TOKEN_KEY, loginResponse.token);

    // Create user object from login response
    const user: CurrentUser = {
      userId: loginResponse.userId,
      email: loginResponse.email || '',
      firstName: loginResponse.firstName || '',
      lastName: loginResponse.lastName || '',
      roles: loginResponse.roles || [],
      isActive: true
    };

    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    return !!token && !this.isTokenExpired(token);
  }

  isAdmin(): boolean {
    const user = this.currentUserSubject.value;
    return user?.roles?.includes('Admin') ?? false;
  }

  hasRole(role: string): boolean {
    const user = this.currentUserSubject.value;
    return user?.roles?.includes(role) ?? false;
  }

  hasAnyRole(roles: string[]): boolean {
    const user = this.currentUserSubject.value;
    if (!user?.roles || roles.length === 0) return false;
    return roles.some(role => user.roles.includes(role));
  }

  hasAllRoles(roles: string[]): boolean {
    const user = this.currentUserSubject.value;
    if (!user?.roles || roles.length === 0) return false;
    return roles.every(role => user.roles.includes(role));
  }

  isUser(): boolean {
    return this.hasRole('User');
  }

  isModerator(): boolean {
    return this.hasRole('Moderator');
  }

  getCurrentUserRoles(): string[] {
    const user = this.currentUserSubject.value;
    return user?.roles ?? [];
  }

  canAccess(requiredRoles: string[], requireAll: boolean = false): boolean {
    if (requiredRoles.length === 0) return true;
    return requireAll ? this.hasAllRoles(requiredRoles) : this.hasAnyRole(requiredRoles);
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const exp = payload.exp * 1000; // Convert to milliseconds
      return Date.now() >= exp;
    } catch (error) {
      return true; // If we can't parse the token, consider it expired
    }
  }

  updateCurrentUser(user: CurrentUser): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  // API Methods using the generated client
  loginUser(email: string, password: string): Observable<LoginResponseDto> {
    const loginDto = new LoginUserDto({ email, password });

    return this.apiClient.login(loginDto)
      .pipe(
        map((response: LoginResponseDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Login failed');
          }
          this.setCurrentUser(response.data);
          return response.data;
        }),
        catchError(error => {
          console.error('Login error:', error);
          return throwError(() => error);
        })
      );
  }

  registerUser(email: string, password: string, firstName?: string, lastName?: string): Observable<UserDto> {
    const registerDto = new RegisterUserDto({ email, password, firstName, lastName });

    return this.apiClient.register(registerDto)
      .pipe(
        map((response: UserDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Registration failed');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Registration error:', error);
          return throwError(() => error);
        })
      );
  }

  getCurrentUser(): Observable<CurrentUser> {
    return this.apiClient.me()
      .pipe(
        map((response: UserDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to get current user');
          }

          const user: CurrentUser = {
            userId: response.data.userId || '',
            email: response.data.email || '',
            firstName: response.data.firstName || '',
            lastName: response.data.lastName || '',
            roles: response.data.roles || [],
            isActive: response.data.isActive ?? true
          };

          this.updateCurrentUser(user);
          return user;
        }),
        catchError(error => {
          console.error('Get current user error:', error);
          return throwError(() => error);
        })
      );
  }
}
