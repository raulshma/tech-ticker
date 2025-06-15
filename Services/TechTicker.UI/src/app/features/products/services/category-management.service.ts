import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, ApiResponse } from '../../../core/services/api.service';

export interface CategoryListItem {
  categoryId: string;
  name: string;
  slug: string;
  description?: string;
  productCount?: number;
  createdAt: string;
  updatedAt: string;
}

export interface CategoryQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
}

export interface PaginatedCategoryResponse {
  items: CategoryListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CategoryDetails {
  categoryId: string;
  name: string;
  slug: string;
  description?: string;
  productCount?: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCategoryRequest {
  name: string;
  slug?: string;
  description?: string;
}

export interface UpdateCategoryRequest {
  name?: string;
  slug?: string;
  description?: string;
}

@Injectable({
  providedIn: 'root'
})
export class CategoryManagementService {

  constructor(private apiService: ApiService) { }

  /**
   * Get paginated list of categories
   */
  getCategories(params?: CategoryQueryParams): Observable<ApiResponse<PaginatedCategoryResponse>> {
    const queryParams = new URLSearchParams();

    if (params?.page) queryParams.append('page', params.page.toString());
    if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    if (params?.search) queryParams.append('search', params.search);

    const queryString = queryParams.toString();
    const url = queryString ? `/categories?${queryString}` : '/categories';

    return this.apiService.get<ApiResponse<PaginatedCategoryResponse>>(url);
  }

  /**
   * Get all categories for dropdowns (without pagination)
   */
  getAllCategories(): Observable<ApiResponse<CategoryListItem[]>> {
    return this.apiService.get<ApiResponse<CategoryListItem[]>>('/categories/all');
  }

  /**
   * Get category details by ID
   */
  getCategoryById(categoryId: string): Observable<ApiResponse<CategoryDetails>> {
    return this.apiService.get<ApiResponse<CategoryDetails>>(`/categories/${categoryId}`);
  }

  /**
   * Create new category
   */
  createCategory(categoryData: CreateCategoryRequest): Observable<ApiResponse<CategoryDetails>> {
    return this.apiService.post<ApiResponse<CategoryDetails>>('/categories', categoryData);
  }

  /**
   * Update category
   */
  updateCategory(categoryId: string, updateData: UpdateCategoryRequest): Observable<ApiResponse<CategoryDetails>> {
    return this.apiService.put<ApiResponse<CategoryDetails>>(`/categories/${categoryId}`, updateData);
  }

  /**
   * Delete category
   */
  deleteCategory(categoryId: string): Observable<ApiResponse<any>> {
    return this.apiService.delete<ApiResponse<any>>(`/categories/${categoryId}`);
  }

  /**
   * Generate slug from name
   */
  generateSlug(name: string): string {
    return name
      .toLowerCase()
      .trim()
      .replace(/[^a-z0-9\s-]/g, '') // Remove special characters
      .replace(/\s+/g, '-') // Replace spaces with hyphens
      .replace(/-+/g, '-') // Replace multiple hyphens with single
      .replace(/^-|-$/g, ''); // Remove leading/trailing hyphens
  }

  /**
   * Check if slug is available
   */
  checkSlugAvailability(slug: string, excludeCategoryId?: string): Observable<ApiResponse<boolean>> {
    const params = new URLSearchParams();
    params.append('slug', slug);
    if (excludeCategoryId) {
      params.append('excludeId', excludeCategoryId);
    }

    return this.apiService.get<ApiResponse<boolean>>(`/categories/check-slug?${params.toString()}`);
  }
}
