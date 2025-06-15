import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';

export interface SiteConfiguration {
  id: string;
  domain: string;
  name: string;
  description?: string;
  selectors: {
    productName?: string;
    price?: string;
    availability?: string;
    image?: string;
    description?: string;
  };
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateSiteConfigurationDto {
  domain: string;
  name: string;
  description?: string;
  selectors: {
    productName?: string;
    price?: string;
    availability?: string;
    image?: string;
    description?: string;
  };
  isActive?: boolean;
}

export interface UpdateSiteConfigurationDto {
  domain?: string;
  name?: string;
  description?: string;
  selectors?: {
    productName?: string;
    price?: string;
    availability?: string;
    image?: string;
    description?: string;
  };
  isActive?: boolean;
}

export interface SiteConfigurationSearchParams {
  domain?: string;
  name?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

@Injectable({
  providedIn: 'root'
})
export class SiteConfigurationService {

  constructor(private apiService: ApiService) { }

  /**
   * Get all site configurations with optional filtering and pagination
   */
  getSiteConfigurations(params?: SiteConfigurationSearchParams): Observable<{
    items: SiteConfiguration[];
    total: number;
    page: number;
    pageSize: number;
  }> {
    return this.apiService.get('/api/site-configs', { params });
  }

  /**
   * Get site configuration by ID
   */
  getSiteConfigurationById(id: string): Observable<SiteConfiguration> {
    return this.apiService.get(`/api/site-configs/${id}`);
  }

  /**
   * Get site configuration by domain
   */
  getSiteConfigurationByDomain(domain: string): Observable<SiteConfiguration> {
    return this.apiService.get('/api/site-configs', {
      params: { domain }
    });
  }

  /**
   * Create a new site configuration
   */
  createSiteConfiguration(data: CreateSiteConfigurationDto): Observable<SiteConfiguration> {
    return this.apiService.post('/api/site-configs', data);
  }

  /**
   * Update an existing site configuration
   */
  updateSiteConfiguration(id: string, data: UpdateSiteConfigurationDto): Observable<SiteConfiguration> {
    return this.apiService.put(`/api/site-configs/${id}`, data);
  }

  /**
   * Delete a site configuration
   */
  deleteSiteConfiguration(id: string): Observable<void> {
    return this.apiService.delete(`/api/site-configs/${id}`);
  }

  /**
   * Validate domain uniqueness
   */
  validateDomain(domain: string, excludeId?: string): Observable<{ isValid: boolean; message?: string }> {
    const params = excludeId ? { domain, excludeId } : { domain };
    return this.apiService.get('/api/site-configs/validate-domain', { params });
  }

  /**
   * Test selectors on a given URL
   */
  testSelectors(domain: string, selectors: any, testUrl: string): Observable<{
    success: boolean;
    results: any;
    errors?: string[];
  }> {
    return this.apiService.post('/api/site-configs/test-selectors', {
      domain,
      selectors,
      testUrl
    });
  }
}
