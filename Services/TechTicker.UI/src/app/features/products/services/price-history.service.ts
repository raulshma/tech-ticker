import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';

export interface PriceHistoryEntry {
  id: string;
  canonicalProductId: string;
  sellerName: string;
  price: number;
  currency: string;
  stockStatus: string;
  scrapedAt: Date;
  sourceUrl: string;
  siteConfigId?: string;
  siteConfig?: {
    domain: string;
    name: string;
  };
}

export interface PriceHistorySearchParams {
  canonicalProductId: string;
  sellerName?: string;
  startDate?: string;
  endDate?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface PriceHistoryStats {
  averagePrice: number;
  minPrice: number;
  maxPrice: number;
  currentPrice?: number;
  priceChange24h?: number;
  priceChangePercent24h?: number;
  totalEntries: number;
  sellersCount: number;
  lastUpdated?: Date;
}

export interface ChartDataPoint {
  date: Date;
  price: number;
  seller: string;
  stockStatus: string;
}

@Injectable({
  providedIn: 'root'
})
export class PriceHistoryService {

  constructor(private apiService: ApiService) { }

  /**
   * Get price history for a canonical product
   */
  getPriceHistory(params: PriceHistorySearchParams): Observable<{
    items: PriceHistoryEntry[];
    total: number;
    page: number;
    pageSize: number;
  }> {
    const { canonicalProductId, ...queryParams } = params;
    return this.apiService.get(`/api/products/${canonicalProductId}/price-history`, {
      params: queryParams
    });
  }

  /**
   * Get price history statistics for a product
   */
  getPriceHistoryStats(canonicalProductId: string, timeRange?: string): Observable<PriceHistoryStats> {
    const params = timeRange ? { timeRange } : {};
    return this.apiService.get(`/api/products/${canonicalProductId}/price-history/stats`, {
      params
    });
  }

  /**
   * Get price history data formatted for charts
   */
  getPriceHistoryForChart(
    canonicalProductId: string,
    timeRange?: string,
    sellerName?: string
  ): Observable<ChartDataPoint[]> {
    const params: any = {};
    if (timeRange) params.timeRange = timeRange;
    if (sellerName) params.sellerName = sellerName;

    return this.apiService.get(`/api/products/${canonicalProductId}/price-history/chart`, {
      params
    });
  }

  /**
   * Get available sellers for a product
   */
  getAvailableSellers(canonicalProductId: string): Observable<string[]> {
    return this.apiService.get(`/api/products/${canonicalProductId}/sellers`);
  }

  /**
   * Get price alerts for a product (if implemented)
   */
  getPriceAlerts(canonicalProductId: string): Observable<any[]> {
    return this.apiService.get(`/api/products/${canonicalProductId}/price-alerts`);
  }

  /**
   * Export price history data
   */
  exportPriceHistory(
    canonicalProductId: string,
    format: 'csv' | 'xlsx' = 'csv',
    filters?: PriceHistorySearchParams
  ): Observable<Blob> {
    const params = {
      format,
      ...filters
    };

    return this.apiService.get(`/api/products/${canonicalProductId}/price-history/export`, {
      params,
      responseType: 'blob'
    });
  }

  /**
   * Get price comparison across sellers
   */
  getPriceComparison(canonicalProductId: string): Observable<{
    seller: string;
    currentPrice: number;
    lastPrice: number;
    priceChange: number;
    priceChangePercent: number;
    lastUpdated: Date;
    stockStatus: string;
  }[]> {
    return this.apiService.get(`/api/products/${canonicalProductId}/price-comparison`);
  }

  /**
   * Get price trend analysis
   */
  getPriceTrend(
    canonicalProductId: string,
    timeRange: '7d' | '30d' | '90d' | '365d' = '30d'
  ): Observable<{
    trend: 'up' | 'down' | 'stable';
    trendPercentage: number;
    recommendation: string;
    confidence: number;
  }> {
    return this.apiService.get(`/api/products/${canonicalProductId}/price-trend`, {
      params: { timeRange }
    });
  }
}
