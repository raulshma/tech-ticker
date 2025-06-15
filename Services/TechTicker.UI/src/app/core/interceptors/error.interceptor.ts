import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        let errorMessage = 'An unknown error occurred';

        if (error.error instanceof ErrorEvent) {
          // Client-side error
          errorMessage = `Client Error: ${error.error.message}`;
        } else {
          // Server-side error
          switch (error.status) {
            case 400:
              errorMessage = 'Bad Request: Please check your input';
              break;
            case 401:
              errorMessage = 'Unauthorized: Please log in';
              // Clear token and redirect to login if needed
              localStorage.removeItem('auth_token');
              break;
            case 403:
              errorMessage = 'Forbidden: You do not have permission';
              break;
            case 404:
              errorMessage = 'Not Found: Resource does not exist';
              break;
            case 500:
              errorMessage = 'Internal Server Error: Please try again later';
              break;
            default:
              errorMessage = `Error ${error.status}: ${error.message}`;
          }
        }

        console.error('HTTP Error:', {
          status: error.status,
          message: errorMessage,
          url: error.url,
          error: error.error
        });

        return throwError(() => ({
          ...error,
          userMessage: errorMessage
        }));
      })
    );
  }
}
