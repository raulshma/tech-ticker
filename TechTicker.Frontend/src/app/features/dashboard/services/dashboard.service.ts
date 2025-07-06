import { Injectable } from '@angular/core';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  TechTickerApiClient,
  DashboardStatsDtoApiResponse,
  BrowserAutomationAnalyticsDtoApiResponse,
  AlertSystemAnalyticsDtoApiResponse,
  ProxyManagementAnalyticsDtoApiResponse,
  ScrapingWorkerAnalyticsDtoApiResponse,
  RealTimeSystemStatusDtoApiResponse,
  RealTimeSystemStatusDto,
  BrowserAutomationAnalyticsDto,
  AlertSystemAnalyticsDto,
  ProxyManagementAnalyticsDto,
  ScrapingWorkerAnalyticsDto
} from '../../../shared/api/api-client';

export interface DashboardStats {
  totalProducts: number;
  totalCategories: number;
  activeMappings: number;
  activeAlerts: number;
  totalUsers?: number;
  totalProxies?: number;
  healthyProxies?: number;
  proxyHealthPercentage?: number;
  recentScraperRuns?: number;
  scraperSuccessRate?: number;
  recentNotifications?: number;
  notificationSuccessRate?: number;
  systemHealthy?: boolean;
  recentAlerts?: number;
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
          const data = response.data;
          return {
            totalProducts: data.totalProducts || 0,
            totalCategories: data.totalCategories || 0,
            activeMappings: data.activeMappings || 0,
            activeAlerts: data.activeAlerts || 0,
            totalUsers: data.totalUsers || undefined,
            totalProxies: data.totalProxies || undefined,
            healthyProxies: data.healthyProxies || undefined,
            proxyHealthPercentage: data.proxyHealthPercentage || undefined,
            recentScraperRuns: data.recentScraperRuns || undefined,
            scraperSuccessRate: data.scraperSuccessRate || undefined,
            recentNotifications: data.recentNotifications || undefined,
            notificationSuccessRate: data.notificationSuccessRate || undefined,
            systemHealthy: data.systemHealthy || undefined,
            recentAlerts: data.recentAlerts || undefined
          };
        }),
        catchError(error => {
          console.error('Error fetching dashboard statistics:', error);
          return throwError(() => error);
        })
      );
  }

  getRealTimeSystemStatus(dateFrom?: Date | null, dateTo?: Date | null): Observable<RealTimeSystemStatusDto> {
    return this.apiClient.getRealTimeSystemStatus()
      .pipe(
        map((response: RealTimeSystemStatusDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch real-time status');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching real-time status:', error);
          return throwError(() => error);
        })
      );
  }

  getBrowserAutomationAnalytics(dateFrom?: Date | null, dateTo?: Date | null): Observable<BrowserAutomationAnalyticsDto> {
    return this.apiClient.getBrowserAutomationAnalytics(dateFrom || undefined, dateTo || undefined)
      .pipe(
        map((response: BrowserAutomationAnalyticsDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch browser automation analytics');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching browser automation analytics:', error);
          return throwError(() => error);
        })
      );
  }

  getAlertSystemAnalytics(dateFrom?: Date | null, dateTo?: Date | null): Observable<AlertSystemAnalyticsDto> {
    return this.apiClient.getAlertSystemAnalytics(dateFrom || undefined, dateTo || undefined)
      .pipe(
        map((response: AlertSystemAnalyticsDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch alert system analytics');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching alert system analytics:', error);
          return throwError(() => error);
        })
      );
  }

  getProxyManagementAnalytics(dateFrom?: Date | null, dateTo?: Date | null): Observable<ProxyManagementAnalyticsDto> {
    return this.apiClient.getProxyManagementAnalytics(dateFrom || undefined, dateTo || undefined)
      .pipe(
        map((response: ProxyManagementAnalyticsDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch proxy management analytics');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching proxy management analytics:', error);
          return throwError(() => error);
        })
      );
  }

  getScrapingWorkerAnalytics(dateFrom?: Date | null, dateTo?: Date | null): Observable<ScrapingWorkerAnalyticsDto> {
    return this.apiClient.getScrapingWorkerAnalytics(dateFrom || undefined, dateTo || undefined)
      .pipe(
        map((response: ScrapingWorkerAnalyticsDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch scraping worker analytics');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching scraping worker analytics:', error);
          return throwError(() => error);
        })
      );
  }
}
