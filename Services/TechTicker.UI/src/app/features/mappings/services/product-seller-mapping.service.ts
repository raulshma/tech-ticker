import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';

export interface ProductSellerMapping {
  id: string;
  canonicalProductId: string;
  canonicalProduct?: {
    id: string;
    name: string;
    sku: string;
    category?: {
      id: string;
      name: string;
    };
  };
  sellerName: string;
  sellerProductUrl: string;
  sellerProductId?: string;
  isActiveForScraping: boolean;
  siteConfigId: string;
  siteConfig?: {
    id: string;
    domain: string;
    name: string;
  };
  scrapingFrequencyMinutes?: number;
  lastScrapedAt?: Date;
  nextScrapeAt?: Date;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateProductSellerMappingDto {
  canonicalProductId: string;
  sellerName: string;
  sellerProductUrl: string;
  sellerProductId?: string;
  isActiveForScraping?: boolean;
  siteConfigId: string;
  scrapingFrequencyMinutes?: number;
}

export interface UpdateProductSellerMappingDto {
  canonicalProductId?: string;
  sellerName?: string;
  sellerProductUrl?: string;
  sellerProductId?: string;
  isActiveForScraping?: boolean;
  siteConfigId?: string;
  scrapingFrequencyMinutes?: number;
}

export interface ProductSellerMappingSearchParams {
  canonicalProductId?: string;
  sellerName?: string;
  siteConfigId?: string;
  isActiveForScraping?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

@Injectable({
  providedIn: 'root'
})
export class ProductSellerMappingService {

  constructor(private apiService: ApiService) { }

  /**
   * Get all product-seller mappings with optional filtering and pagination
   */
  getMappings(params?: ProductSellerMappingSearchParams): Observable<{
    items: ProductSellerMapping[];
    total: number;
    page: number;
    pageSize: number;
  }> {
    return this.apiService.get('/api/mappings', { params });
  }

  /**
   * Get mapping by ID
   */
  getMappingById(id: string): Observable<ProductSellerMapping> {
    return this.apiService.get(`/api/mappings/${id}`);
  }

  /**
   * Get mappings by canonical product ID
   */
  getMappingsByCanonicalProductId(canonicalProductId: string): Observable<ProductSellerMapping[]> {
    return this.apiService.get('/api/mappings', {
      params: { canonicalProductId }
    });
  }

  /**
   * Create a new product-seller mapping
   */
  createMapping(data: CreateProductSellerMappingDto): Observable<ProductSellerMapping> {
    return this.apiService.post('/api/mappings', data);
  }

  /**
   * Update an existing product-seller mapping
   */
  updateMapping(id: string, data: UpdateProductSellerMappingDto): Observable<ProductSellerMapping> {
    return this.apiService.put(`/api/mappings/${id}`, data);
  }

  /**
   * Delete a product-seller mapping
   */
  deleteMapping(id: string): Observable<void> {
    return this.apiService.delete(`/api/mappings/${id}`);
  }

  /**
   * Toggle scraping status for a mapping
   */
  toggleScrapingStatus(id: string, isActive: boolean): Observable<ProductSellerMapping> {
    return this.apiService.patch(`/api/mappings/${id}/scraping-status`, { isActiveForScraping: isActive });
  }

  /**
   * Trigger immediate scraping for a mapping
   */
  triggerScraping(id: string): Observable<{ success: boolean; message: string }> {
    return this.apiService.post(`/api/mappings/${id}/trigger-scraping`, {});
  }

  /**
   * Get scraping statistics for a mapping
   */
  getScrapingStats(id: string): Observable<{
    totalScrapes: number;
    successfulScrapes: number;
    failedScrapes: number;
    lastScrapedAt?: Date;
    nextScrapeAt?: Date;
    averageScrapingTime?: number;
  }> {
    return this.apiService.get(`/api/mappings/${id}/scraping-stats`);
  }

  /**
   * Validate seller product URL
   */
  validateSellerUrl(url: string, siteConfigId: string): Observable<{
    isValid: boolean;
    canScrape: boolean;
    message?: string;
    previewData?: any;
  }> {
    return this.apiService.post('/api/mappings/validate-url', {
      url,
      siteConfigId
    });
  }

  /**
   * Search for canonical products (for dropdown selection)
   */
  searchCanonicalProducts(searchTerm: string): Observable<{
    id: string;
    name: string;
    sku: string;
    category?: { name: string };
  }[]> {
    return this.apiService.get('/api/products/search', {
      params: {
        search: searchTerm,
        pageSize: 20
      }
    });
  }

  /**
   * Get available site configurations (for dropdown selection)
   */
  getAvailableSiteConfigs(): Observable<{
    id: string;
    domain: string;
    name: string;
    isActive: boolean;
  }[]> {
    return this.apiService.get('/api/site-configs', {
      params: {
        isActive: true,
        pageSize: 100
      }
    });
  }
}
