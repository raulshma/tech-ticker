import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, catchError, throwError, tap } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { TechTickerApiClient, LoginUserDto, RegisterUserDto } from '../api/api-client';
import { environment } from '../../../environments/environment';

export interface CurrentUser {
  userId: string;
  email: string;
  firstName?: string;
  lastName?: string;
  roles: string[];
  isActive: boolean;
}

export interface LoginResponse {
  token: string;
  userId: string;
  email: string;
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
    private apiClient: TechTickerApiClient,
    private http: HttpClient
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

  login(loginResponse: LoginResponse): void {
    localStorage.setItem(this.TOKEN_KEY, loginResponse.token);

    // Create user object from login response
    const user: CurrentUser = {
      userId: loginResponse.userId,
      email: loginResponse.email,
      firstName: '',
      lastName: '',
      roles: [],
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
  loginUser(email: string, password: string): Observable<LoginResponse> {
    const loginDto = new LoginUserDto({ email, password });

    // Since the generated client returns Observable<void>, we'll use direct HTTP call
    // This is a workaround until the API properly returns typed responses
    return this.http.post<LoginResponse>(`${environment.apiUrl}/Auth/login`, loginDto.toJSON())
      .pipe(
        tap(response => this.login(response)),
        catchError(error => {
          console.error('Login error:', error);
          return throwError(() => error);
        })
      );
  }

  registerUser(email: string, password: string, firstName?: string, lastName?: string): Observable<void> {
    const registerDto = new RegisterUserDto({ email, password, firstName, lastName });

    // Using direct HTTP call for now
    return this.http.post<void>(`${environment.apiUrl}/Auth/register`, registerDto.toJSON())
      .pipe(
        catchError(error => {
          console.error('Registration error:', error);
          return throwError(() => error);
        })
      );
  }

  getCurrentUser(): Observable<CurrentUser> {
    // Using direct HTTP call for now
    return this.http.get<CurrentUser>(`${environment.apiUrl}/Auth/me`)
      .pipe(
        tap(user => this.updateCurrentUser(user)),
        catchError(error => {
          console.error('Get current user error:', error);
          return throwError(() => error);
        })
      );
  }
}
