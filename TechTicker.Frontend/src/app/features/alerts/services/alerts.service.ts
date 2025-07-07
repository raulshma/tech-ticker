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
  ApiResponse,
  AlertTestResultDto,
  AlertTestResultDtoApiResponse,
  TestPricePointDto,
  AlertTestRequestDto,
  AlertRuleSimulationRequestDto,
  AlertPerformanceMetricsDto,
  AlertPerformanceMetricsDtoApiResponse,
  AlertRuleValidationResultDto,
  AlertRuleValidationResultDtoApiResponse,
  TestAlertRuleDto
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

  // Alert testing methods
  testAlert(alertRuleId: string, testPricePoint: TestPricePointDto): Observable<AlertTestResultDto> {
    return this.apiClient.testAlertRule(alertRuleId, testPricePoint)
      .pipe(
        map((response: AlertTestResultDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to test alert');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error testing alert:', error);
          return throwError(() => error);
        })
      );
  }

  testAlertAgainstHistory(request: AlertTestRequestDto): Observable<AlertTestResultDto> {
    return this.apiClient.testAlertRuleAgainstHistory(request)
      .pipe(
        map((response: AlertTestResultDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to test alert against history');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error testing alert against history:', error);
          return throwError(() => error);
        })
      );
  }

  simulateAlert(request: AlertRuleSimulationRequestDto): Observable<AlertTestResultDto> {
    return this.apiClient.simulateAlertRule(request)
      .pipe(
        map((response: AlertTestResultDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to simulate alert');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error simulating alert:', error);
          return throwError(() => error);
        })
      );
  }

  getAlertPerformance(alertRuleId: string, startDate?: Date, endDate?: Date): Observable<AlertPerformanceMetricsDto> {
    return this.apiClient.getAlertRulePerformance(alertRuleId)
      .pipe(
        map((response: AlertPerformanceMetricsDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to get alert performance');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error getting alert performance:', error);
          return throwError(() => error);
        })
      );
  }

  validateAlert(alertRule: TestAlertRuleDto): Observable<AlertRuleValidationResultDto> {
    return this.apiClient.validateAlertRule(alertRule)
      .pipe(
        map((response: AlertRuleValidationResultDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to validate alert');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error validating alert:', error);
          return throwError(() => error);
        })
      );
  }
}
