import { Injectable } from '@angular/core';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  TechTickerApiClient,
  ProductDto,
  CreateProductDto,
  UpdateProductDto,
  ProductDtoApiResponse,
  ProductDtoPagedResponse,
  ApiResponse
} from '../../../shared/api/api-client';

export interface ProductsFilter {
  categoryId?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

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
export class ProductsService {

  constructor(private apiClient: TechTickerApiClient) {}

  getProducts(filter: ProductsFilter = {}): Observable<PagedResult<ProductDto>> {
    return this.apiClient.getProducts(
      filter.categoryId,
      filter.search,
      filter.page || 1,
      filter.pageSize || 10
    ).pipe(
      map((response: ProductDtoPagedResponse) => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to fetch products');
        }
        return {
          items: response.data || [],
          totalCount: response.pagination?.totalCount || 0,
          page: response.pagination?.pageNumber || 1,
          pageSize: response.pagination?.pageSize || 10,
          totalPages: response.pagination?.totalPages || 0
        };
      }),
      catchError(error => {
        console.error('Error fetching products:', error);
        return throwError(() => error);
      })
    );
  }

  getProduct(productId: string): Observable<ProductDto> {
    return this.apiClient.getProductById(productId).pipe(
      map((response: ProductDtoApiResponse) => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to fetch product');
        }
        return response.data;
      }),
      catchError(error => {
        console.error('Error fetching product:', error);
        return throwError(() => error);
      })
    );
  }



  createProduct(product: CreateProductDto): Observable<ProductDto> {
    return this.apiClient.createProduct(product)
      .pipe(
        map((response: ProductDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to create product');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error creating product:', error);
          return throwError(() => error);
        })
      );
  }

  updateProduct(id: string, product: UpdateProductDto): Observable<ProductDto> {
    return this.apiClient.updateProduct(id, product)
      .pipe(
        map((response: ProductDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to update product');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error updating product:', error);
          return throwError(() => error);
        })
      );
  }

  deleteProduct(id: string): Observable<void> {
    return this.apiClient.deleteProduct(id)
      .pipe(
        map(() => void 0),
        catchError(error => {
          console.error('Error deleting product:', error);
          return throwError(() => error);
        })
      );
  }

  // Helper method for alert form autocomplete
  getAllProducts(): Observable<ProductDto[]> {
    return this.getProducts({ pageSize: 1000 }).pipe(
      map(result => result.items)
    );
  }

  searchProducts(query: string): Observable<ProductDto[]> {
    return this.getProducts({ search: query, pageSize: 20 }).pipe(
      map(result => result.items)
    );
  }
}
