import { Injectable } from '@angular/core';
import { Observable, map, catchError, throwError, BehaviorSubject, of } from 'rxjs';
import { 
  TechTickerApiClient, 
  ScraperSiteConfigurationDto, 
  CreateScraperSiteConfigurationDto, 
  UpdateScraperSiteConfigurationDto,
  ScraperSiteConfigurationDtoApiResponse,
  ApiResponse
} from '../../../shared/api/api-client';

@Injectable({
  providedIn: 'root'
})
export class SiteConfigsService {
  private siteConfigsSubject = new BehaviorSubject<ScraperSiteConfigurationDto[]>([]);
  public siteConfigs$ = this.siteConfigsSubject.asObservable();

  constructor(private apiClient: TechTickerApiClient) {}

  // Since there's no "get all" endpoint, we'll maintain a local list
  // In a real application, you might want to implement a backend endpoint for this
  getSiteConfigs(): Observable<ScraperSiteConfigurationDto[]> {
    return this.siteConfigs$;
  }

  getSiteConfig(id: string): Observable<ScraperSiteConfigurationDto> {
    return this.apiClient.siteConfigsGET2(id)
      .pipe(
        map((response: ScraperSiteConfigurationDtoApiResponse) => {
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

  createSiteConfig(siteConfig: CreateScraperSiteConfigurationDto): Observable<ScraperSiteConfigurationDto> {
    return this.apiClient.siteConfigsPOST(siteConfig)
      .pipe(
        map((response: ScraperSiteConfigurationDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to create site configuration');
          }
          
          // Add to local list
          const currentConfigs = this.siteConfigsSubject.value;
          this.siteConfigsSubject.next([...currentConfigs, response.data]);
          
          return response.data;
        }),
        catchError(error => {
          console.error('Error creating site configuration:', error);
          return throwError(() => error);
        })
      );
  }

  updateSiteConfig(id: string, siteConfig: UpdateScraperSiteConfigurationDto): Observable<ScraperSiteConfigurationDto> {
    return this.apiClient.siteConfigsPUT(id, siteConfig)
      .pipe(
        map((response: ScraperSiteConfigurationDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to update site configuration');
          }
          
          // Update in local list
          const currentConfigs = this.siteConfigsSubject.value;
          const updatedConfigs = currentConfigs.map(config => 
            config.siteConfigId === id ? response.data! : config
          );
          this.siteConfigsSubject.next(updatedConfigs);
          
          return response.data;
        }),
        catchError(error => {
          console.error('Error updating site configuration:', error);
          return throwError(() => error);
        })
      );
  }

  deleteSiteConfig(id: string): Observable<void> {
    return this.apiClient.siteConfigsDELETE(id)
      .pipe(
        map(() => {
          // Remove from local list
          const currentConfigs = this.siteConfigsSubject.value;
          const filteredConfigs = currentConfigs.filter(config => config.siteConfigId !== id);
          this.siteConfigsSubject.next(filteredConfigs);
          
          return void 0;
        }),
        catchError(error => {
          console.error('Error deleting site configuration:', error);
          return throwError(() => error);
        })
      );
  }

  // Helper method to add a site config to the local list (for when loaded individually)
  addToLocalList(siteConfig: ScraperSiteConfigurationDto): void {
    const currentConfigs = this.siteConfigsSubject.value;
    const exists = currentConfigs.some(config => config.siteConfigId === siteConfig.siteConfigId);
    
    if (!exists) {
      this.siteConfigsSubject.next([...currentConfigs, siteConfig]);
    }
  }

  // Method to initialize with known configurations (could be called from app initialization)
  initializeWithConfigs(configs: ScraperSiteConfigurationDto[]): void {
    this.siteConfigsSubject.next(configs);
  }
}
