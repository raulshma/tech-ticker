import { Injectable } from '@angular/core';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  TechTickerApiClient,
  CategoryDto,
  CreateCategoryDto,
  UpdateCategoryDto,
  CategoryDtoApiResponse,
  CategoryDtoIEnumerableApiResponse
} from '../../../shared/api/api-client';

@Injectable({
  providedIn: 'root'
})
export class CategoriesService {

  constructor(private apiClient: TechTickerApiClient) {}

  getCategories(): Observable<CategoryDto[]> {
    return this.apiClient.categoriesGET()
      .pipe(
        map((response: CategoryDtoIEnumerableApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch categories');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching categories:', error);
          return throwError(() => error);
        })
      );
  }

  getCategory(id: string): Observable<CategoryDto> {
    return this.apiClient.categoriesGET2(id)
      .pipe(
        map((response: CategoryDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch category');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching category:', error);
          return throwError(() => error);
        })
      );
  }

  createCategory(category: CreateCategoryDto): Observable<CategoryDto> {
    return this.apiClient.categoriesPOST(category)
      .pipe(
        map((response: CategoryDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to create category');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error creating category:', error);
          return throwError(() => error);
        })
      );
  }

  updateCategory(id: string, category: UpdateCategoryDto): Observable<CategoryDto> {
    return this.apiClient.categoriesPUT(id, category)
      .pipe(
        map((response: CategoryDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to update category');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error updating category:', error);
          return throwError(() => error);
        })
      );
  }

  deleteCategory(id: string): Observable<void> {
    return this.apiClient.categoriesDELETE(id)
      .pipe(
        map(() => void 0),
        catchError(error => {
          console.error('Error deleting category:', error);
          return throwError(() => error);
        })
      );
  }
}
