import { Injectable } from '@angular/core';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  TechTickerApiClient,
  DashboardStatsDtoApiResponse,
  AnalyticsDashboardDtoApiResponse
} from '../../../shared/api/api-client';

export interface DashboardStats {
  totalProducts: number;
  totalCategories: number;
  activeMappings: number;
  activeAlerts: number;
  totalUsers?: number;
  totalProxies: number;
  healthyProxies: number;
  proxyHealthPercentage: number;
  recentScraperRuns: number;
  scraperSuccessRate: number;
  recentNotifications: number;
  notificationSuccessRate: number;
  systemHealthy: boolean;
  recentAlerts: number;
}

export interface AnalyticsDashboardData {
  browserAutomation: {
    successRateTrend: any[];
    executionTimeTrend: any[];
    commonFailurePoints: any[];
    browserReliability: { [key: string]: number };
    popularTestUrls: any[];
    flakyTests: any[];
    overallStatistics: {
      totalExecutions: number;
      successfulExecutions: number;
      successRate: number;
      averageExecutionTime: number;
      firstExecution?: Date;
      lastExecution?: Date;
      uniqueUrls: number;
      uniqueProfiles: number;
    };
  };
  alertSystem: {
    triggerFrequency: any[];
    notificationSuccessRate: any[];
    responseTimeTrend: any[];
    topPerformers: any[];
    poorPerformers: any[];
    systemHealth: {
      isHealthy: boolean;
      healthIssues: string[];
      activeAlertRules: number;
      inactiveAlertRules: number;
      alertRulesWithErrors: number;
      systemUptime: number;
      memoryUsageMB: number;
      cpuUsagePercent: number;
      queueBacklog: number;
      averageProcessingDelay: number;
      overallSuccessRate: number;
      averageResponseTime: number;
      alertsInLastHour: number;
      notificationsInLastHour: number;
    };
  };
  proxyManagement: {
    healthStatusTrend: any[];
    failureRateTrend: any[];
    performanceImpact: any[];
    usageStatistics: {
      totalProxies: number;
      activeProxies: number;
      healthyProxies: number;
      healthPercentage: number;
      averageResponseTime: number;
      overallSuccessRate: number;
      totalRequests: number;
      failedRequests: number;
    };
  };
  scrapingWorker: {
    successRateTrend: any[];
    scrapingTimeTrend: any[];
    frequentlyScrapedProducts: any[];
    sellerPerformance: any[];
    overallStatistics: {
      totalScrapes: number;
      successfulScrapes: number;
      successRate: number;
      averageScrapingTime: number;
      uniqueProducts: number;
      uniqueSellers: number;
      firstScrape?: Date;
      lastScrape?: Date;
    };
  };
  systemWideMetrics: {
    totalProducts: number;
    totalCategories: number;
    activeMappings: number;
    activeAlerts: number;
    totalUsers: number;
    totalProxies: number;
    systemUptime: number;
    overallHealthScore: number;
  };
  realTimeStatus: {
    systemHealthy: boolean;
    recentAlerts: number;
    recentNotifications: number;
    notificationSuccessRate: number;
    recentScraperRuns: number;
    scraperSuccessRate: number;
    healthyProxies: number;
    proxyHealthPercentage: number;
    activeIssues: string[];
    lastUpdated: Date;
  };
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
            totalUsers: response.data.totalUsers || undefined,
            totalProxies: response.data.totalProxies || 0,
            healthyProxies: response.data.healthyProxies || 0,
            proxyHealthPercentage: response.data.proxyHealthPercentage || 0,
            recentScraperRuns: response.data.recentScraperRuns || 0,
            scraperSuccessRate: response.data.scraperSuccessRate || 0,
            recentNotifications: response.data.recentNotifications || 0,
            notificationSuccessRate: response.data.notificationSuccessRate || 0,
            systemHealthy: response.data.systemHealthy || false,
            recentAlerts: response.data.recentAlerts || 0
          };
        }),
        catchError(error => {
          console.error('Error fetching dashboard statistics:', error);
          return throwError(() => error);
        })
      );
  }

  getAnalyticsDashboard(dateFrom?: Date | null, dateTo?: Date | null): Observable<AnalyticsDashboardData> {
    return this.apiClient.getAnalyticsDashboard(dateFrom || undefined, dateTo || undefined)
      .pipe(
        map((response: AnalyticsDashboardDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch analytics dashboard data');
          }
          
          const data = response.data;
          return {
            browserAutomation: {
              successRateTrend: data.browserAutomation?.successRateTrend || [],
              executionTimeTrend: data.browserAutomation?.executionTimeTrend || [],
              commonFailurePoints: data.browserAutomation?.commonFailurePoints || [],
              browserReliability: data.browserAutomation?.browserReliability || {},
              popularTestUrls: data.browserAutomation?.popularTestUrls || [],
              flakyTests: data.browserAutomation?.flakyTests || [],
              overallStatistics: {
                totalExecutions: data.browserAutomation?.overallStatistics?.totalExecutions || 0,
                successfulExecutions: data.browserAutomation?.overallStatistics?.successfulExecutions || 0,
                successRate: data.browserAutomation?.overallStatistics?.successRate || 0,
                averageExecutionTime: data.browserAutomation?.overallStatistics?.averageExecutionTime || 0,
                firstExecution: data.browserAutomation?.overallStatistics?.firstExecution ? new Date(data.browserAutomation.overallStatistics.firstExecution) : undefined,
                lastExecution: data.browserAutomation?.overallStatistics?.lastExecution ? new Date(data.browserAutomation.overallStatistics.lastExecution) : undefined,
                uniqueUrls: data.browserAutomation?.overallStatistics?.uniqueUrls || 0,
                uniqueProfiles: data.browserAutomation?.overallStatistics?.uniqueProfiles || 0
              }
            },
            alertSystem: {
              triggerFrequency: data.alertSystem?.triggerFrequency || [],
              notificationSuccessRate: data.alertSystem?.notificationSuccessRate || [],
              responseTimeTrend: data.alertSystem?.responseTimeTrend || [],
              topPerformers: data.alertSystem?.topPerformers || [],
              poorPerformers: data.alertSystem?.poorPerformers || [],
              systemHealth: {
                isHealthy: data.alertSystem?.systemHealth?.isHealthy || false,
                healthIssues: data.alertSystem?.systemHealth?.healthIssues || [],
                activeAlertRules: data.alertSystem?.systemHealth?.activeAlertRules || 0,
                inactiveAlertRules: data.alertSystem?.systemHealth?.inactiveAlertRules || 0,
                alertRulesWithErrors: data.alertSystem?.systemHealth?.alertRulesWithErrors || 0,
                systemUptime: typeof data.alertSystem?.systemHealth?.systemUptime === 'number' ? data.alertSystem.systemHealth.systemUptime : 0,
                memoryUsageMB: data.alertSystem?.systemHealth?.memoryUsageMB || 0,
                cpuUsagePercent: data.alertSystem?.systemHealth?.cpuUsagePercent || 0,
                queueBacklog: data.alertSystem?.systemHealth?.queueBacklog || 0,
                averageProcessingDelay: typeof data.alertSystem?.systemHealth?.averageProcessingDelay === 'number' ? data.alertSystem.systemHealth.averageProcessingDelay : 0,
                overallSuccessRate: data.alertSystem?.systemHealth?.overallSuccessRate || 0,
                averageResponseTime: data.alertSystem?.systemHealth?.averageResponseTime || 0,
                alertsInLastHour: data.alertSystem?.systemHealth?.alertsInLastHour || 0,
                notificationsInLastHour: data.alertSystem?.systemHealth?.notificationsInLastHour || 0
              }
            },
            proxyManagement: {
              healthStatusTrend: data.proxyManagement?.healthStatusTrend || [],
              failureRateTrend: data.proxyManagement?.failureRateTrend || [],
              performanceImpact: data.proxyManagement?.performanceImpact || [],
              usageStatistics: {
                totalProxies: data.proxyManagement?.usageStatistics?.totalProxies || 0,
                activeProxies: data.proxyManagement?.usageStatistics?.activeProxies || 0,
                healthyProxies: data.proxyManagement?.usageStatistics?.healthyProxies || 0,
                healthPercentage: data.proxyManagement?.usageStatistics?.healthPercentage || 0,
                averageResponseTime: data.proxyManagement?.usageStatistics?.averageResponseTime || 0,
                overallSuccessRate: data.proxyManagement?.usageStatistics?.overallSuccessRate || 0,
                totalRequests: data.proxyManagement?.usageStatistics?.totalRequests || 0,
                failedRequests: data.proxyManagement?.usageStatistics?.failedRequests || 0
              }
            },
            scrapingWorker: {
              successRateTrend: data.scrapingWorker?.successRateTrend || [],
              scrapingTimeTrend: data.scrapingWorker?.scrapingTimeTrend || [],
              frequentlyScrapedProducts: data.scrapingWorker?.frequentlyScrapedProducts || [],
              sellerPerformance: data.scrapingWorker?.sellerPerformance || [],
              overallStatistics: {
                totalScrapes: data.scrapingWorker?.overallStatistics?.totalScrapes || 0,
                successfulScrapes: data.scrapingWorker?.overallStatistics?.successfulScrapes || 0,
                successRate: data.scrapingWorker?.overallStatistics?.successRate || 0,
                averageScrapingTime: data.scrapingWorker?.overallStatistics?.averageScrapingTime || 0,
                uniqueProducts: data.scrapingWorker?.overallStatistics?.uniqueProducts || 0,
                uniqueSellers: data.scrapingWorker?.overallStatistics?.uniqueSellers || 0,
                firstScrape: data.scrapingWorker?.overallStatistics?.firstScrape ? new Date(data.scrapingWorker.overallStatistics.firstScrape) : undefined,
                lastScrape: data.scrapingWorker?.overallStatistics?.lastScrape ? new Date(data.scrapingWorker.overallStatistics.lastScrape) : undefined
              }
            },
            systemWideMetrics: {
              totalProducts: data.systemWideMetrics?.totalProducts || 0,
              totalCategories: data.systemWideMetrics?.totalCategories || 0,
              activeMappings: data.systemWideMetrics?.activeMappings || 0,
              activeAlerts: data.systemWideMetrics?.activeAlerts || 0,
              totalUsers: data.systemWideMetrics?.totalUsers || 0,
              totalProxies: data.systemWideMetrics?.totalProxies || 0,
              systemUptime: data.systemWideMetrics?.systemUptime || 0,
              overallHealthScore: data.systemWideMetrics?.overallHealthScore || 0
            },
            realTimeStatus: {
              systemHealthy: data.realTimeStatus?.systemHealthy || false,
              recentAlerts: data.realTimeStatus?.recentAlerts || 0,
              recentNotifications: data.realTimeStatus?.recentNotifications || 0,
              notificationSuccessRate: data.realTimeStatus?.notificationSuccessRate || 0,
              recentScraperRuns: data.realTimeStatus?.recentScraperRuns || 0,
              scraperSuccessRate: data.realTimeStatus?.scraperSuccessRate || 0,
              healthyProxies: data.realTimeStatus?.healthyProxies || 0,
              proxyHealthPercentage: data.realTimeStatus?.proxyHealthPercentage || 0,
              activeIssues: data.realTimeStatus?.activeIssues || [],
              lastUpdated: data.realTimeStatus?.lastUpdated ? new Date(data.realTimeStatus.lastUpdated) : new Date()
            }
          };
        }),
        catchError(error => {
          console.error('Error fetching analytics dashboard data:', error);
          return throwError(() => error);
        })
      );
  }
}
