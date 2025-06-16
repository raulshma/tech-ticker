import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  TechTickerApiClient,
  ProductSellerMappingDto,
  CreateProductSellerMappingDto,
  UpdateProductSellerMappingDto,
  ProductSellerMappingDtoApiResponse,
  ProductSellerMappingDtoIEnumerableApiResponse,
  ScraperSiteConfigurationDto,
  ScraperSiteConfigurationDtoApiResponse,
  ApiResponse
} from '../../../shared/api/api-client';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MappingsService {

  constructor(
    private apiClient: TechTickerApiClient,
    private http: HttpClient
  ) {}

  getMappings(canonicalProductId?: string): Observable<ProductSellerMappingDto[]> {
    return this.apiClient.mappingsGET(canonicalProductId)
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
    return this.apiClient.mappingsPOST(mapping)
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
    return this.apiClient.mappingsPUT(id, mapping)
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
    return this.apiClient.mappingsDELETE(id)
      .pipe(
        map(() => void 0),
        catchError(error => {
          console.error('Error deleting mapping:', error);
          return throwError(() => error);
        })
      );
  }

  triggerManualScraping(mappingId: string): Observable<string> {
    // For now, we'll use a direct HTTP call since the NSwag client hasn't been regenerated yet
    // This will be replaced with the generated client method later
    const url = `${environment.apiUrl}/mappings/${mappingId}/scrape-now`;
    return this.http.post<ApiResponse>(url, {})
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
    // Note: This might need to be updated based on actual API structure
    // For now, we'll create a simple method that can be expanded later
    return this.apiClient.siteConfigsGET(undefined)
      .pipe(
        map((response: ScraperSiteConfigurationDtoApiResponse) => {
          if (!response.success || !response.data) {
            return [];
          }
          return [response.data]; // Single item for now
        }),
        catchError(error => {
          console.error('Error fetching site configurations:', error);
          return throwError(() => error);
        })
      );
  }
}
