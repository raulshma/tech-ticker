import { Injectable } from '@angular/core';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  TechTickerApiClient,
  SiteConfigurationDto,
  SaveSiteConfigurationRequest,
  SiteConfigurationDtoApiResponse,
  SiteConfigurationDtoPagedResponse,
  SiteConfigurationDtoListApiResponse,
  GenerateSelectorsRequest,
  TestSelectorsRequest,
  SelectorImprovementRequest,
  SelectorGenerationResult,
  SelectorTestResult,
  SelectorSuggestion,
  SelectorGenerationResultApiResponse,
  SelectorTestResultApiResponse,
  SelectorSuggestionListApiResponse,
  ApiResponse
} from '../../../shared/api/api-client';

// Re-export types for convenience
export type {
  SiteConfigurationDto,
  SaveSiteConfigurationRequest,
  GenerateSelectorsRequest,
  TestSelectorsRequest,
  SelectorImprovementRequest,
  SelectorGenerationResult,
  SelectorTestResult,
  SelectorSuggestion
} from '../../../shared/api/api-client';

export interface SiteConfigurationFilterRequest {
  domain?: string;
  siteName?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class SiteConfigurationService {

  constructor(private apiClient: TechTickerApiClient) {}

  /**
   * Gets all site configurations with filtering and pagination
   */
  getConfigurations(filter: SiteConfigurationFilterRequest = {}): Observable<SiteConfigurationDtoPagedResponse> {
    return this.apiClient.siteConfigurationGET(
      filter.domain,
      filter.siteName,
      filter.isActive,
      filter.page,
      filter.pageSize,
      filter.sortBy,
      filter.sortDescending
    ).pipe(
      catchError(error => {
        console.error('Error fetching site configurations:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Gets a specific site configuration by ID
   */
  getConfiguration(id: string): Observable<SiteConfigurationDto> {
    return this.apiClient.siteConfigurationGET2(id)
      .pipe(
        map((response: SiteConfigurationDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch site configuration');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching site configuration:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Gets a site configuration by domain
   */
  getConfigurationByDomain(domain: string): Observable<SiteConfigurationDto> {
    return this.apiClient.byDomain(domain)
      .pipe(
        map((response: SiteConfigurationDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch site configuration');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching site configuration by domain:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Gets all active site configurations
   */
  getActiveConfigurations(): Observable<SiteConfigurationDto[]> {
    return this.apiClient.active2()
      .pipe(
        map((response: SiteConfigurationDtoListApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch active site configurations');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching active site configurations:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Creates a new site configuration
   */
  createConfiguration(request: SaveSiteConfigurationRequest): Observable<SiteConfigurationDto> {
    return this.apiClient.siteConfigurationPOST(request)
      .pipe(
        map((response: SiteConfigurationDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to create site configuration');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error creating site configuration:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Updates an existing site configuration
   */
  updateConfiguration(id: string, request: SaveSiteConfigurationRequest): Observable<SiteConfigurationDto> {
    return this.apiClient.siteConfigurationPUT(id, request)
      .pipe(
        map((response: SiteConfigurationDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to update site configuration');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error updating site configuration:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Deletes a site configuration
   */
  deleteConfiguration(id: string): Observable<void> {
    return this.apiClient.siteConfigurationDELETE(id)
      .pipe(
        map((response: ApiResponse) => {
          if (!response.success) {
            throw new Error(response.message || 'Failed to delete site configuration');
          }
          return void 0;
        }),
        catchError(error => {
          console.error('Error deleting site configuration:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Generates CSS selectors from HTML content using AI
   */
  generateSelectors(request: GenerateSelectorsRequest): Observable<SelectorGenerationResult> {
    return this.apiClient.generateSelectors(request)
      .pipe(
        map((response: SelectorGenerationResultApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to generate selectors');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error generating selectors:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Tests CSS selectors against HTML content
   */
  testSelectors(request: TestSelectorsRequest): Observable<SelectorTestResult> {
    return this.apiClient.testSelectors(request)
      .pipe(
        map((response: SelectorTestResultApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to test selectors');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error testing selectors:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Gets improvement suggestions for CSS selectors
   */
  suggestImprovements(request: SelectorImprovementRequest): Observable<SelectorSuggestion[]> {
    return this.apiClient.suggestImprovements(request)
      .pipe(
        map((response: SelectorSuggestionListApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to get selector suggestions');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error getting selector suggestions:', error);
          return throwError(() => error);
        })
      );
  }
}
