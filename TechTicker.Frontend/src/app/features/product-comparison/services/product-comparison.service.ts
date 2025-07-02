import { Injectable } from '@angular/core';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  TechTickerApiClient,
  ProductDto,
  ProductDtoPagedResponseApiResponse,
  BooleanApiResponse,
  ProductComparisonResultDto,
  ProductComparisonResultDtoApiResponse,
  CompareProductsRequestDto
} from '../../../shared/api/api-client';

// Re-export types from API client for easy access by components
export {
  ProductComparisonResultDto,
  ProductComparisonResultDtoApiResponse,
  CompareProductsRequestDto,
  ProductWithCurrentPricesDto,
  SpecificationComparisonDto,
  PriceAnalysisDto,
  RecommendationAnalysisDto,
  ProductComparisonSummaryDto,
  CurrentPriceDto,
  SpecificationDifferenceDto,
  SpecificationMatchDto,
  CategoryScoreDto,
  PriceComparisonSummaryDto,
  SellerPriceComparisonDto,
  RecommendationFactorDto,
  ComparisonResultType
} from '../../../shared/api/api-client';

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
export class ProductComparisonService {

  constructor(private apiClient: TechTickerApiClient) {}

  /**
   * Compare two products with comprehensive analysis
   */
  compareProducts(request: CompareProductsRequestDto): Observable<ProductComparisonResultDto> {
    return this.apiClient.compareProducts(request).pipe(
      map((response: ProductComparisonResultDtoApiResponse) => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to compare products');
        }
        return response.data;
      }),
      catchError((error) => {
        console.error('Error comparing products:', error);
        return throwError(() => new Error('Failed to compare products. Please try again.'));
      })
    );
  }

  /**
   * Validate if two products can be compared
   */
  validateComparison(productId1: string, productId2: string): Observable<boolean> {
    return this.apiClient.validateProductsForComparison(productId1, productId2).pipe(
      map((response: BooleanApiResponse) => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to validate comparison');
        }
        return response.data || false;
      }),
      catchError((error) => {
        console.error('Error validating comparison:', error);
        return throwError(() => new Error('Failed to validate comparison. Please try again.'));
      })
    );
  }

  /**
   * Get products that can be compared with a given product (same category)
   */
  getComparableProducts(
    productId: string,
    search?: string,
    page: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<ProductDto>> {
    return this.apiClient.getComparableProducts(productId, search, page, pageSize).pipe(
      map((response: ProductDtoPagedResponseApiResponse) => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to fetch comparable products');
        }
        return {
          items: response.data.data || [],
          totalCount: response.data.pagination?.totalCount || 0,
          page: response.data.pagination?.pageNumber || 1,
          pageSize: response.data.pagination?.pageSize || 10,
          totalPages: response.data.pagination?.totalPages || 0
        };
      }),
      catchError((error) => {
        console.error('Error fetching comparable products:', error);
        return throwError(() => new Error('Failed to fetch comparable products. Please try again.'));
      })
    );
  }
}
