import { Injectable } from '@angular/core';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  TechTickerApiClient,
  UserDto,
  CreateUserDto,
  UpdateUserDto,
  UserDtoApiResponse,
  UserDtoPagedResponse,
  ApiResponse,
  PaginationMeta
} from '../../../shared/api/api-client';

export interface UsersListParams {
  page?: number;
  pageSize?: number;
}

export interface UsersListResult {
  items: UserDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root'
})
export class UsersService {

  constructor(private apiClient: TechTickerApiClient) {}

  getUsers(params: UsersListParams = {}): Observable<UsersListResult> {
    const page = params.page || 1;
    const pageSize = params.pageSize || 10;

    return this.apiClient.usersGET(page, pageSize)
      .pipe(
        map((response: UserDtoPagedResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch users');
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
          console.error('Error fetching users:', error);
          return throwError(() => error);
        })
      );
  }

  getUser(id: string): Observable<UserDto> {
    return this.apiClient.usersGET2(id)
      .pipe(
        map((response: UserDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch user');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching user:', error);
          return throwError(() => error);
        })
      );
  }

  createUser(user: CreateUserDto): Observable<UserDto> {
    return this.apiClient.usersPOST(user)
      .pipe(
        map((response: UserDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to create user');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error creating user:', error);
          return throwError(() => error);
        })
      );
  }

  updateUser(id: string, user: UpdateUserDto): Observable<UserDto> {
    return this.apiClient.usersPUT(id, user)
      .pipe(
        map((response: UserDtoApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to update user');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error updating user:', error);
          return throwError(() => error);
        })
      );
  }

  deleteUser(id: string): Observable<void> {
    return this.apiClient.usersDELETE(id)
      .pipe(
        map(() => void 0),
        catchError(error => {
          console.error('Error deleting user:', error);
          return throwError(() => error);
        })
      );
  }

  // Helper method to get available roles
  getAvailableRoles(): string[] {
    return ['Admin', 'User', 'Moderator'];
  }

  // Get role display names
  getRoleDisplayNames(): { [key: string]: string } {
    return {
      'Admin': 'Administrator',
      'User': 'User',
      'Moderator': 'Moderator'
    };
  }
}
