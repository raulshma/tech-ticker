import { Injectable } from '@angular/core';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  TechTickerApiClient,
  AlertRuleDto,
  CreateAlertRuleDto,
  UpdateAlertRuleDto,
  AlertRuleDtoApiResponse,
  AlertRuleDtoIEnumerableApiResponse,
  AlertRuleDtoPagedResponse,
  ApiResponse
} from '../../../shared/api/api-client';

@Injectable({
  providedIn: 'root'
})
export class AlertsService {

  constructor(private apiClient: TechTickerApiClient) { }

  getUserAlerts(): Observable<AlertRuleDto[]> {
    return this.apiClient.getUserAlerts()
      .pipe(
        map((response: AlertRuleDtoIEnumerableApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch user alerts');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching user alerts:', error);
          return throwError(() => error);
        })
      );
  }

  getProductAlerts(productId: string): Observable<AlertRuleDto[]> {
    return this.apiClient.getProductAlerts(productId)
      .pipe(
        map((response: AlertRuleDtoIEnumerableApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch product alerts');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching product alerts:', error);
          return throwError(() => error);
        })
      );
  }

  createAlert(alertRule: CreateAlertRuleDto): Observable<AlertRuleDto> {
    return this.apiClient.createAlert(alertRule)
      .pipe(
        map((response: AlertRuleDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to create alert');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error creating alert:', error);
          return throwError(() => error);
        })
      );
  }

  updateAlert(alertRuleId: string, updates: UpdateAlertRuleDto): Observable<AlertRuleDto> {
    return this.apiClient.updateAlert(alertRuleId, updates)
      .pipe(
        map((response: AlertRuleDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to update alert');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error updating alert:', error);
          return throwError(() => error);
        })
      );
  }

  deleteAlert(alertRuleId: string): Observable<void> {
    return this.apiClient.deleteAlert(alertRuleId)
      .pipe(
        map(() => void 0),
        catchError(error => {
          console.error('Error deleting alert:', error);
          return throwError(() => error);
        })
      );
  }

  // Admin methods
  getAllAlerts(userId?: string, productId?: string, page?: number, pageSize?: number): Observable<AlertRuleDto[]> {
    return this.apiClient.adminGetAllAlerts(userId, productId, page, pageSize)
      .pipe(
        map((response: AlertRuleDtoPagedResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch all alerts');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching all alerts:', error);
          return throwError(() => error);
        })
      );
  }
}
