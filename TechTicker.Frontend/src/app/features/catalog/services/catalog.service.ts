import { Injectable } from '@angular/core';
import { Observable, map, catchError, throwError, of } from 'rxjs';
import {
  TechTickerApiClient,
  ProductWithCurrentPricesDto,
  CurrentPriceDto,
  ProductWithCurrentPricesDtoPagedResponse,
  ProductWithCurrentPricesDtoApiResponse,
  CurrentPriceDtoIEnumerableApiResponse
} from '../../../shared/api/api-client';

export interface CatalogFilter {
  categoryId?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root'
})
export class CatalogService {

  constructor(private apiClient: TechTickerApiClient) {}

  getProductsCatalog(filter: CatalogFilter = {}): Observable<PagedResult<ProductWithCurrentPricesDto>> {
    return this.apiClient.getProductsCatalog(
      filter.categoryId,
      filter.search,
      filter.page || 1,
      filter.pageSize || 12
    ).pipe(
      map((response) => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to fetch products catalog');
        }
        return {
          items: response.data || [],
          totalCount: response.pagination?.totalCount || 0,
          page: response.pagination?.pageNumber || 1,
          pageSize: response.pagination?.pageSize || 12,
          totalPages: response.pagination?.totalPages || 0
        };
      }),
      catchError(error => {
        console.error('Error fetching products catalog:', error);
        return throwError(() => error);
      })
    );
  }

  getProductDetail(productId: string): Observable<ProductWithCurrentPricesDto> {
    return this.apiClient.getProductCatalogDetail(productId)
      .pipe(
        map((response) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch product detail');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching product detail:', error);
          return throwError(() => error);
        })
      );
  }

  getCurrentPrices(productId: string): Observable<CurrentPriceDto[]> {
    return this.apiClient.getCurrentPrices(productId)
      .pipe(
        map((response) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch current prices');
          }
          return response.data || [];
        }),
        catchError(error => {
          console.error('Error fetching current prices:', error);
          return throwError(() => error);
        })
      );
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(price);
  }

  extractSellerName(sourceUrl: string): string {
    try {
      const url = new URL(sourceUrl);
      return url.hostname.replace('www.', '');
    } catch {
      return 'Unknown';
    }
  }

  getStockStatusColor(stockStatus: string): string {
    switch (stockStatus.toLowerCase()) {
      case 'in stock':
      case 'available':
        return 'success';
      case 'out of stock':
      case 'unavailable':
        return 'warn';
      case 'limited stock':
      case 'low stock':
        return 'accent';
      default:
        return 'primary';
    }
  }

  getStockStatusIcon(stockStatus: string): string {
    switch (stockStatus.toLowerCase()) {
      case 'in stock':
      case 'available':
        return 'check_circle';
      case 'out of stock':
      case 'unavailable':
        return 'cancel';
      case 'limited stock':
      case 'low stock':
        return 'warning';
      default:
        return 'help';
    }
  }
}
