import { Injectable } from '@angular/core';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  TechTickerApiClient,
  DashboardStatsDtoApiResponse
} from '../../../shared/api/api-client';

export interface DashboardStats {
  totalProducts: number;
  totalCategories: number;
  activeMappings: number;
  activeAlerts: number;
  totalUsers?: number;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {

  constructor(private apiClient: TechTickerApiClient) {}

  getDashboardStats(): Observable<DashboardStats> {
    return this.apiClient.getDashboardStats()
      .pipe(
        map((response: DashboardStatsDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch dashboard statistics');
          }
          return {
            totalProducts: response.data.totalProducts || 0,
            totalCategories: response.data.totalCategories || 0,
            activeMappings: response.data.activeMappings || 0,
            activeAlerts: response.data.activeAlerts || 0,
            totalUsers: response.data.totalUsers || undefined
          };
        }),
        catchError(error => {
          console.error('Error fetching dashboard statistics:', error);
          return throwError(() => error);
        })
      );
  }
}
