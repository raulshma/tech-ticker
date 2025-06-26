import { Injectable } from '@angular/core';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  TechTickerApiClient,
  PriceHistoryDto,
  PriceHistoryDtoIEnumerableApiResponse
} from '../../../shared/api/api-client';

export interface PriceHistoryFilter {
  sellerName?: string;
  startDate?: Date;
  endDate?: Date;
  limit?: number;
}

export interface PriceHistoryChartData {
  labels: string[];
  datasets: {
    label: string;
    data: number[];
    borderColor: string;
    backgroundColor: string;
    fill: boolean;
  }[];
}

@Injectable({
  providedIn: 'root'
})
export class PriceHistoryService {

  constructor(private apiClient: TechTickerApiClient) {}

  getPriceHistory(productId: string, filter: PriceHistoryFilter = {}): Observable<PriceHistoryDto[]> {
    return this.apiClient.getPriceHistory(
      productId,
      filter.sellerName,
      filter.startDate,
      filter.endDate,
      filter.limit
    ).pipe(
      map((response: PriceHistoryDtoIEnumerableApiResponse) => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to fetch price history');
        }
        return response.data || [];
      }),
      catchError(error => {
        console.error('Error fetching price history:', error);
        return throwError(() => error);
      })
    );
  }

  transformToChartData(priceHistory: PriceHistoryDto[]): PriceHistoryChartData {
    if (!priceHistory || priceHistory.length === 0) {
      return {
        labels: [],
        datasets: []
      };
    }

    // Sort by timestamp
    const sortedHistory = [...priceHistory].sort((a, b) =>
      new Date(a.timestamp!).getTime() - new Date(b.timestamp!).getTime()
    );

    // Group by seller name for multiple datasets
    const sellerGroups = new Map<string, { timestamps: Date[], prices: number[] }>();

    sortedHistory.forEach(item => {
      const sellerName = this.extractSellerName(item.sourceUrl || 'Unknown');
      if (!sellerGroups.has(sellerName)) {
        sellerGroups.set(sellerName, { timestamps: [], prices: [] });
      }

      const group = sellerGroups.get(sellerName)!;
      group.timestamps.push(new Date(item.timestamp!));
      group.prices.push(item.price || 0);
    });

    // Create labels from all unique timestamps
    const allTimestamps = [...new Set(sortedHistory.map(item =>
      new Date(item.timestamp!).toLocaleDateString()
    ))];

    // Generate colors for different sellers
    const colors = [
      '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0',
      '#9966FF', '#FF9F40', '#FF6384', '#C9CBCF'
    ];

    const datasets = Array.from(sellerGroups.entries()).map(([sellerName, data], index) => ({
      label: sellerName,
      data: data.prices,
      borderColor: colors[index % colors.length],
      backgroundColor: colors[index % colors.length] + '20',
      fill: false
    }));

    return {
      labels: allTimestamps,
      datasets
    };
  }

  private extractSellerName(sourceUrl: string): string {
    try {
      const url = new URL(sourceUrl);
      return url.hostname.replace('www.', '');
    } catch {
      return 'Unknown';
    }
  }

  getUniqueSellerNames(priceHistory: PriceHistoryDto[]): string[] {
    const sellerNames = new Set<string>();
    priceHistory.forEach(item => {
      const sellerName = this.extractSellerName(item.sourceUrl || 'Unknown');
      sellerNames.add(sellerName);
    });
    return Array.from(sellerNames).sort();
  }
}
