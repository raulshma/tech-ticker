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
  PaginationMeta,
  PermissionDto,
  PermissionDtoApiResponse,
  PermissionDtoIEnumerableApiResponse,
  BooleanApiResponse,
  StringStringArrayDictionaryApiResponse
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

export interface PermissionCategory {
  name: string;
  permissions: PermissionDto[];
}

@Injectable({
  providedIn: 'root'
})
export class UsersService {

  constructor(private apiClient: TechTickerApiClient) {}

  getUsers(params: UsersListParams = {}): Observable<UsersListResult> {
    const page = params.page || 1;
    const pageSize = params.pageSize || 10;

    return this.apiClient.getAllUsers(page, pageSize)
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
    return this.apiClient.getUserById(id)
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
    return this.apiClient.createUser(user)
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
    return this.apiClient.updateUser(id, user)
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
    return this.apiClient.deleteUser(id)
      .pipe(
        map(() => void 0),
        catchError(error => {
          console.error('Error deleting user:', error);
          return throwError(() => error);
        })
      );
  }

  // Permission management methods
  getUserPermissions(userId: string): Observable<PermissionDto[]> {
    return this.apiClient.getUserPermissions(userId)
      .pipe(
        map((response: PermissionDtoIEnumerableApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch user permissions');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching user permissions:', error);
          return throwError(() => error);
        })
      );
  }

  getAssignablePermissions(): Observable<PermissionDto[]> {
    return this.apiClient.getAllPermissions()
      .pipe(
        map((response: PermissionDtoIEnumerableApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch assignable permissions');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching assignable permissions:', error);
          return throwError(() => error);
        })
      );
  }

  assignPermissionToUser(userId: string, permissionId: string): Observable<boolean> {
    return this.apiClient.assignPermissionToUser(userId, permissionId)
      .pipe(
        map((response: BooleanApiResponse) => {
          if (!response.success) {
            throw new Error(response.message || 'Failed to assign permission to user');
          }
          return response.data || false;
        }),
        catchError(error => {
          console.error('Error assigning permission to user:', error);
          return throwError(() => error);
        })
      );
  }

  removePermissionFromUser(userId: string, permissionId: string): Observable<boolean> {
    return this.apiClient.removePermissionFromUser(userId, permissionId)
      .pipe(
        map((response: BooleanApiResponse) => {
          if (!response.success) {
            throw new Error(response.message || 'Failed to remove permission from user');
          }
          return response.data || false;
        }),
        catchError(error => {
          console.error('Error removing permission from user:', error);
          return throwError(() => error);
        })
      );
  }

  bulkAssignPermissionsToUser(userId: string, permissionIds: string[]): Observable<boolean> {
    return this.apiClient.bulkAssignPermissionsToUser(userId, permissionIds)
      .pipe(
        map((response: BooleanApiResponse) => {
          if (!response.success) {
            throw new Error(response.message || 'Failed to bulk assign permissions to user');
          }
          return response.data || false;
        }),
        catchError(error => {
          console.error('Error bulk assigning permissions to user:', error);
          return throwError(() => error);
        })
      );
  }

  getPermissionCategories(): Observable<{ [key: string]: string[] }> {
    return this.apiClient.getPermissionCategories()
      .pipe(
        map((response: StringStringArrayDictionaryApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch permission categories');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching permission categories:', error);
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

  // Helper method to group permissions by category
  groupPermissionsByCategory(permissions: PermissionDto[]): PermissionCategory[] {
    const grouped = permissions.reduce((acc, permission) => {
      const category = permission.category || 'Other';
      if (!acc[category]) {
        acc[category] = [];
      }
      acc[category].push(permission);
      return acc;
    }, {} as { [key: string]: PermissionDto[] });

    return Object.keys(grouped).map(category => ({
      name: category,
      permissions: grouped[category].sort((a, b) => (a.name || '').localeCompare(b.name || ''))
    }));
  }
}
