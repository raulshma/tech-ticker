import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, ApiResponse } from '../../../core/services/api.service';

export interface ProductListItem {
  productId: string;
  name: string;
  sku?: string;
  manufacturer?: string;
  modelNumber?: string;
  categoryId: string;
  categoryName: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ProductQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: string;
  isActive?: boolean;
  manufacturer?: string;
}

export interface PaginatedProductResponse {
  items: ProductListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ProductDetails {
  productId: string;
  name: string;
  sku?: string;
  manufacturer?: string;
  modelNumber?: string;
  categoryId: string;
  categoryName: string;
  description?: string;
  specifications?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProductRequest {
  name: string;
  sku?: string;
  manufacturer?: string;
  modelNumber?: string;
  categoryId: string;
  description?: string;
  specifications?: string;
  isActive?: boolean;
}

export interface UpdateProductRequest {
  name?: string;
  sku?: string;
  manufacturer?: string;
  modelNumber?: string;
  categoryId?: string;
  description?: string;
  specifications?: string;
  isActive?: boolean;
}

export interface ProductExportOptions {
  format: 'csv' | 'excel';
  includeInactive?: boolean;
  categoryId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ProductManagementService {

  constructor(private apiService: ApiService) { }

  /**
   * Get paginated list of products
   */
  getProducts(params?: ProductQueryParams): Observable<ApiResponse<PaginatedProductResponse>> {
    const queryParams = new URLSearchParams();

    if (params?.page) queryParams.append('page', params.page.toString());
    if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    if (params?.search) queryParams.append('search', params.search);
    if (params?.categoryId) queryParams.append('categoryId', params.categoryId);
    if (params?.isActive !== undefined) queryParams.append('isActive', params.isActive.toString());
    if (params?.manufacturer) queryParams.append('manufacturer', params.manufacturer);

    const queryString = queryParams.toString();
    const url = queryString ? `/products?${queryString}` : '/products';

    return this.apiService.get<ApiResponse<PaginatedProductResponse>>(url);
  }

  /**
   * Get product details by ID
   */
  getProductById(productId: string): Observable<ApiResponse<ProductDetails>> {
    return this.apiService.get<ApiResponse<ProductDetails>>(`/products/${productId}`);
  }

  /**
   * Create new product
   */
  createProduct(productData: CreateProductRequest): Observable<ApiResponse<ProductDetails>> {
    return this.apiService.post<ApiResponse<ProductDetails>>('/products', productData);
  }

  /**
   * Update product
   */
  updateProduct(productId: string, updateData: UpdateProductRequest): Observable<ApiResponse<ProductDetails>> {
    return this.apiService.put<ApiResponse<ProductDetails>>(`/products/${productId}`, updateData);
  }

  /**
   * Delete product
   */
  deleteProduct(productId: string): Observable<ApiResponse<any>> {
    return this.apiService.delete<ApiResponse<any>>(`/products/${productId}`);
  }

  /**
   * Bulk delete products
   */
  bulkDeleteProducts(productIds: string[]): Observable<ApiResponse<any>> {
    return this.apiService.post<ApiResponse<any>>('/products/bulk-delete', { productIds });
  }

  /**
   * Export products
   */
  exportProducts(options: ProductExportOptions): Observable<Blob> {
    const queryParams = new URLSearchParams();
    queryParams.append('format', options.format);
    if (options.includeInactive) queryParams.append('includeInactive', 'true');
    if (options.categoryId) queryParams.append('categoryId', options.categoryId);

    return this.apiService.getBlob(`/products/export?${queryParams.toString()}`);
  }

  /**
   * Check if SKU is available
   */
  checkSkuAvailability(sku: string, excludeProductId?: string): Observable<ApiResponse<boolean>> {
    const params = new URLSearchParams();
    params.append('sku', sku);
    if (excludeProductId) {
      params.append('excludeId', excludeProductId);
    }

    return this.apiService.get<ApiResponse<boolean>>(`/products/check-sku?${params.toString()}`);
  }

  /**
   * Get unique manufacturers for filters
   */
  getManufacturers(): Observable<ApiResponse<string[]>> {
    return this.apiService.get<ApiResponse<string[]>>('/products/manufacturers');
  }

  /**
   * Toggle product active status
   */
  toggleProductStatus(productId: string, isActive: boolean): Observable<ApiResponse<ProductDetails>> {
    return this.apiService.patch<ApiResponse<ProductDetails>>(`/products/${productId}/status`, { isActive });
  }

  /**
   * Bulk update product status
   */
  bulkUpdateProductStatus(productIds: string[], isActive: boolean): Observable<ApiResponse<any>> {
    return this.apiService.post<ApiResponse<any>>('/products/bulk-status', { productIds, isActive });
  }
}
