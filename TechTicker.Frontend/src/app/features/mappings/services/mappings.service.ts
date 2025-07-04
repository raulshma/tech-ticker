import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, catchError, throwError, of } from 'rxjs';
import {
  TechTickerApiClient,
  ProductSellerMappingDto,
  CreateProductSellerMappingDto,
  UpdateProductSellerMappingDto,
  ProductSellerMappingDtoApiResponse,
  ProductSellerMappingDtoIEnumerableApiResponse,
  ScraperSiteConfigurationDto,
  ApiResponse
} from '../../../shared/api/api-client';

// Temporary DTOs until API client is regenerated
export interface BulkCreateProductSellerMappingDto {
  sellerName: string;
  exactProductUrl: string;
  isActiveForScraping?: boolean;
  scrapingFrequencyOverride?: string;
  siteConfigId?: string;
}

export interface BulkUpdateProductSellerMappingDto {
  mappingId: string;
  sellerName?: string;
  exactProductUrl?: string;
  isActiveForScraping?: boolean;
  scrapingFrequencyOverride?: string;
  siteConfigId?: string;
}

export interface ProductSellerMappingBulkUpdateDto {
  create: BulkCreateProductSellerMappingDto[];
  update: BulkUpdateProductSellerMappingDto[];
  deleteIds: string[];
}

@Injectable({
  providedIn: 'root'
})
export class MappingsService {

  constructor(
    private apiClient: TechTickerApiClient,
    private http: HttpClient
  ) {}

  getMappings(canonicalProductId?: string, isActiveForScraping?: boolean): Observable<ProductSellerMappingDto[]> {
    return this.apiClient.getMappings(canonicalProductId, isActiveForScraping)
      .pipe(
        map((response: ProductSellerMappingDtoIEnumerableApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch mappings');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching mappings:', error);
          return throwError(() => error);
        })
      );
  }

  getMapping(id: string): Observable<ProductSellerMappingDto> {
    // Since there's no individual mapping endpoint, we'll get all mappings and filter
    return this.getMappings().pipe(
      map(mappings => {
        const mapping = mappings.find(m => m.mappingId === id);
        if (!mapping) {
          throw new Error('Mapping not found');
        }
        return mapping;
      })
    );
  }

  createMapping(mapping: CreateProductSellerMappingDto): Observable<ProductSellerMappingDto> {
    return this.apiClient.createMapping(mapping)
      .pipe(
        map((response: ProductSellerMappingDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to create mapping');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error creating mapping:', error);
          return throwError(() => error);
        })
      );
  }

  updateMapping(id: string, mapping: UpdateProductSellerMappingDto): Observable<ProductSellerMappingDto> {
    return this.apiClient.updateMapping(id, mapping)
      .pipe(
        map((response: ProductSellerMappingDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to update mapping');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error updating mapping:', error);
          return throwError(() => error);
        })
      );
  }

  deleteMapping(id: string): Observable<void> {
    return this.apiClient.deleteMapping(id)
      .pipe(
        map(() => void 0),
        catchError(error => {
          console.error('Error deleting mapping:', error);
          return throwError(() => error);
        })
      );
  }

  triggerManualScraping(mappingId: string): Observable<string> {
    return this.apiClient.triggerManualScraping(mappingId)
      .pipe(
        map((response: ApiResponse) => {
          if (!response.success) {
            throw new Error(response.message || 'Failed to trigger scraping');
          }
          return response.message || 'Scraping job has been queued successfully.';
        }),
        catchError(error => {
          console.error('Error triggering manual scraping:', error);
          return throwError(() => error);
        })
      );
  }

  // Helper method to get site configurations for dropdowns
  getSiteConfigurations(): Observable<ScraperSiteConfigurationDto[]> {
    return this.apiClient.getAllSiteConfigs()
      .pipe(
        map((response) => {
          if (!response.success || !response.data) {
            return [];
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching site configurations:', error);
          return of([]); // Return empty array on error instead of throwing
        })
      );
  }

  // Bulk update method for product mappings
  bulkUpdateProductMappings(productId: string, bulkUpdateDto: ProductSellerMappingBulkUpdateDto): Observable<ProductSellerMappingDto[]> {
    const url = `/api/mappings/products/${productId}/bulk`;
    return this.http.post<ProductSellerMappingDtoIEnumerableApiResponse>(url, bulkUpdateDto)
      .pipe(
        map((response: ProductSellerMappingDtoIEnumerableApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to update mappings');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error bulk updating mappings:', error);
          return throwError(() => error);
        })
      );
  }
}
