import { Injectable } from '@angular/core';
import { Observable, map, catchError, throwError } from 'rxjs';
import { TechTickerApiClient, SystemEventRequestDto } from '../../../shared/api/api-client';

@Injectable({
  providedIn: 'root'
})
export class AlertPerformanceService {

  constructor(private apiClient: TechTickerApiClient) { }

  getSystemPerformance(startDate?: Date, endDate?: Date): Observable<any> {
    return this.apiClient.getSystemPerformance(startDate, endDate)
      .pipe(
        map((response: any) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch system performance');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching system performance:', error);
          return throwError(() => error);
        })
      );
  }

  getSystemHealth(): Observable<any> {
    return this.apiClient.getSystemHealth()
      .pipe(
        map((response: any) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch system health');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching system health:', error);
          return throwError(() => error);
        })
      );
  }

  getRealTimeMonitoring(): Observable<any> {
    return this.apiClient.getRealTimeMonitoring()
      .pipe(
        map((response: any) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch real-time monitoring');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching real-time monitoring:', error);
          return throwError(() => error);
        })
      );
  }

  getPerformanceTrends(startDate: Date, endDate: Date, intervalHours: number = 1): Observable<any> {
    return this.apiClient.getPerformanceTrends(startDate, endDate, intervalHours)
      .pipe(
        map((response: any) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch performance trends');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching performance trends:', error);
          return throwError(() => error);
        })
      );
  }

  getTopPerformingAlertRules(count: number = 10, startDate?: Date, endDate?: Date): Observable<any> {
    return this.apiClient.getTopPerformingAlertRules(count, startDate, endDate)
      .pipe(
        map((response: any) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch top performing alert rules');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching top performing alert rules:', error);
          return throwError(() => error);
        })
      );
  }

  getPoorlyPerformingAlertRules(count: number = 10, startDate?: Date, endDate?: Date): Observable<any> {
    return this.apiClient.getPoorlyPerformingAlertRules(count, startDate, endDate)
      .pipe(
        map((response: any) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch poorly performing alert rules');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching poorly performing alert rules:', error);
          return throwError(() => error);
        })
      );
  }

  generatePerformanceReport(startDate: Date, endDate: Date): Observable<any> {
    return this.apiClient.generatePerformanceReport(startDate, endDate)
      .pipe(
        map((response: any) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to generate performance report');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error generating performance report:', error);
          return throwError(() => error);
        })
      );
  }

  getNotificationChannelStats(startDate?: Date, endDate?: Date): Observable<any> {
    return this.apiClient.getNotificationChannelStats(startDate, endDate)
      .pipe(
        map((response: any) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch notification channel stats');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching notification channel stats:', error);
          return throwError(() => error);
        })
      );
  }

  recordSystemEvent(eventType: string, message: string, component?: string, metadata?: any): Observable<any> {
    const request = new SystemEventRequestDto({
      eventType,
      message,
      component,
      metadata
    });

    return this.apiClient.recordSystemEvent(request)
      .pipe(
        map((response: any) => {
          if (!response.success) {
            throw new Error(response.message || 'Failed to record system event');
          }
          return response;
        }),
        catchError(error => {
          console.error('Error recording system event:', error);
          return throwError(() => error);
        })
      );
  }
}
