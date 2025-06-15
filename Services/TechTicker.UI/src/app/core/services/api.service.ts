import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, retry } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface ApiResponse<T = any> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    // Add auth token if available
    const token = this.getAuthToken();
    if (token) {
      return headers.set('Authorization', `Bearer ${token}`);
    }

    return headers;
  }

  private getAuthToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An unknown error occurred';

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Error: ${error.error.message}`;
    } else {
      // Server-side error
      errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
    }

    console.error('API Error:', errorMessage);
    return throwError(() => error);
  }

  // Generic HTTP methods
  get<T>(endpoint: string, params?: any): Observable<T> {
    const url = `${this.baseUrl}${endpoint}`;
    return this.http.get<T>(url, {
      headers: this.getHeaders(),
      params
    }).pipe(
      retry(1),
      catchError(this.handleError)
    );
  }

  post<T>(endpoint: string, body: any): Observable<T> {
    const url = `${this.baseUrl}${endpoint}`;
    return this.http.post<T>(url, body, {
      headers: this.getHeaders()
    }).pipe(
      catchError(this.handleError)
    );
  }

  put<T>(endpoint: string, body: any): Observable<T> {
    const url = `${this.baseUrl}${endpoint}`;
    return this.http.put<T>(url, body, {
      headers: this.getHeaders()
    }).pipe(
      catchError(this.handleError)
    );
  }

  delete<T>(endpoint: string): Observable<T> {
    const url = `${this.baseUrl}${endpoint}`;
    return this.http.delete<T>(url, {
      headers: this.getHeaders()
    }).pipe(
      catchError(this.handleError)
    );
  }

  patch<T>(endpoint: string, body: any): Observable<T> {
    const url = `${this.baseUrl}${endpoint}`;
    return this.http.patch<T>(url, body, {
      headers: this.getHeaders()
    }).pipe(
      catchError(this.handleError)
    );
  }

  getBlob(endpoint: string, params?: any): Observable<Blob> {
    const url = `${this.baseUrl}${endpoint}`;
    return this.http.get(url, {
      headers: this.getHeaders(),
      params,
      responseType: 'blob'
    }).pipe(
      catchError(this.handleError)
    );
  }

  // Convenience methods for common API patterns
  getById<T>(endpoint: string, id: string | number): Observable<T> {
    return this.get<T>(`${endpoint}/${id}`);
  }

  search<T>(endpoint: string, searchParams: any): Observable<T> {
    return this.get<T>(`${endpoint}/search`, searchParams);
  }
}
