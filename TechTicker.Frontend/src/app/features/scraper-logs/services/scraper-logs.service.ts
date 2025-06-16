import { Injectable } from '@angular/core';
import { Observable, map, catchError, of } from 'rxjs';
import { 
  TechTickerApiClient, 
  ScraperRunLogSummaryDto, 
  ScraperRunLogDto,
  ScraperRunLogSummaryDtoPagedResultDto 
} from '../../../shared/api/api-client';

export interface ScraperLogFilter {
  page: number;
  pageSize: number;
  mappingId?: string;
  status?: string;
  errorCategory?: string;
  dateFrom?: Date;
  dateTo?: Date;
  sellerName?: string;
}

export interface ScraperLogPagedResult {
  items: ScraperRunLogSummaryDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ScraperLogsService {

  constructor(private apiClient: TechTickerApiClient) { }

  /**
   * Get paginated list of scraper run logs with filtering
   */
  getScraperLogs(filter: ScraperLogFilter): Observable<ScraperLogPagedResult | null> {
    return this.apiClient.scraperLogs(
      filter.page,
      filter.pageSize,
      filter.mappingId,
      filter.status,
      filter.errorCategory,
      filter.dateFrom,
      filter.dateTo,
      filter.sellerName
    ).pipe(
      map(response => {
        if (response.success && response.data) {
          return {
            items: response.data.items || [],
            totalCount: response.data.totalCount || 0,
            page: response.data.page || 1,
            pageSize: response.data.pageSize || 10,
            totalPages: response.data.totalPages || 0,
            hasNextPage: response.data.hasNextPage || false,
            hasPreviousPage: response.data.hasPreviousPage || false
          };
        }
        return null;
      }),
      catchError(error => {
        console.error('Error fetching scraper logs:', error);
        return of(null);
      })
    );
  }

  /**
   * Get detailed information for a specific scraper run log
   */
  getScraperLogDetail(runId: string): Observable<ScraperRunLogDto | null> {
    return this.apiClient.scraperLogs2(runId).pipe(
      map(response => {
        if (response.success && response.data) {
          return response.data;
        }
        return null;
      }),
      catchError(error => {
        console.error('Error fetching scraper log detail:', error);
        return of(null);
      })
    );
  }

  /**
   * Get scraper logs for a specific mapping
   */
  getScraperLogsByMapping(mappingId: string, page: number = 1, pageSize: number = 10): Observable<ScraperLogPagedResult | null> {
    return this.apiClient.mapping(mappingId, page, pageSize).pipe(
      map(response => {
        if (response.success && response.data) {
          return {
            items: response.data.items || [],
            totalCount: response.data.totalCount || 0,
            page: response.data.page || 1,
            pageSize: response.data.pageSize || 10,
            totalPages: response.data.totalPages || 0,
            hasNextPage: response.data.hasNextPage || false,
            hasPreviousPage: response.data.hasPreviousPage || false
          };
        }
        return null;
      }),
      catchError(error => {
        console.error('Error fetching scraper logs by mapping:', error);
        return of(null);
      })
    );
  }

  /**
   * Get recent failed scraper runs
   */
  getRecentFailures(count: number = 10): Observable<ScraperRunLogSummaryDto[]> {
    return this.apiClient.recentFailures(count).pipe(
      map(response => {
        if (response.success && response.data) {
          return response.data;
        }
        return [];
      }),
      catchError(error => {
        console.error('Error fetching recent failures:', error);
        return of([]);
      })
    );
  }

  /**
   * Get recent scraper runs for a specific mapping
   */
  getRecentRunsByMapping(mappingId: string, count: number = 10): Observable<ScraperRunLogSummaryDto[]> {
    return this.apiClient.recent(mappingId, count).pipe(
      map(response => {
        if (response.success && response.data) {
          return response.data;
        }
        return [];
      }),
      catchError(error => {
        console.error('Error fetching recent runs by mapping:', error);
        return of([]);
      })
    );
  }

  /**
   * Get retry chain for a specific run
   */
  getRetryChain(runId: string): Observable<ScraperRunLogDto[]> {
    return this.apiClient.retryChain(runId).pipe(
      map(response => {
        if (response.success && response.data) {
          return response.data;
        }
        return [];
      }),
      catchError(error => {
        console.error('Error fetching retry chain:', error);
        return of([]);
      })
    );
  }

  /**
   * Get currently in-progress scraper runs
   */
  getInProgressRuns(): Observable<ScraperRunLogSummaryDto[]> {
    return this.apiClient.inProgress().pipe(
      map(response => {
        if (response.success && response.data) {
          return response.data;
        }
        return [];
      }),
      catchError(error => {
        console.error('Error fetching in-progress runs:', error);
        return of([]);
      })
    );
  }

  /**
   * Helper method to format duration
   */
  formatDuration(duration?: string): string {
    if (!duration) return 'N/A';
    
    // Parse ISO 8601 duration format (PT1M30S)
    const match = duration.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?/);
    if (!match) return duration;

    const hours = parseInt(match[1] || '0');
    const minutes = parseInt(match[2] || '0');
    const seconds = parseFloat(match[3] || '0');

    const parts = [];
    if (hours > 0) parts.push(`${hours}h`);
    if (minutes > 0) parts.push(`${minutes}m`);
    if (seconds > 0) parts.push(`${seconds.toFixed(1)}s`);

    return parts.length > 0 ? parts.join(' ') : '0s';
  }

  /**
   * Helper method to get status color
   */
  getStatusColor(status?: string): string {
    switch (status?.toUpperCase()) {
      case 'SUCCESS': return 'success';
      case 'FAILED': return 'warn';
      case 'STARTED': 
      case 'IN_PROGRESS': return 'primary';
      case 'TIMEOUT': return 'accent';
      case 'CANCELLED': return 'disabled';
      default: return 'basic';
    }
  }

  /**
   * Helper method to get status icon
   */
  getStatusIcon(status?: string): string {
    switch (status?.toUpperCase()) {
      case 'SUCCESS': return 'check_circle';
      case 'FAILED': return 'error';
      case 'STARTED': 
      case 'IN_PROGRESS': return 'hourglass_empty';
      case 'TIMEOUT': return 'schedule';
      case 'CANCELLED': return 'cancel';
      default: return 'help';
    }
  }
}
