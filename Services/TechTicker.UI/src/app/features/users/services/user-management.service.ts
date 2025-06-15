import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, ApiResponse } from '../../../core/services/api.service';

export interface UserListItem {
  userId: string;
  email: string;
  firstName?: string;
  lastName?: string;
  isActive: boolean;
  emailConfirmed: boolean;
  lastLoginAt?: string;
  roles: string[];
  createdAt: string;
  updatedAt: string;
}

export interface UserQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
  emailConfirmed?: boolean;
}

export interface PaginatedUserResponse {
  items: UserListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface UserDetails {
  userId: string;
  email: string;
  firstName?: string;
  lastName?: string;
  isActive: boolean;
  emailConfirmed: boolean;
  lastLoginAt?: string;
  roles: string[];
  createdAt: string;
  updatedAt: string;
}

export interface UpdateUserRequest {
  firstName?: string;
  lastName?: string;
}

export interface AssignRoleRequest {
  userId: string;
  roleName: string;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
}

export interface RoleInfo {
  roleId: string;
  name: string;
  description?: string;
  permissions: string[];
  createdAt: string;
  updatedAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserManagementService {

  constructor(private apiService: ApiService) { }

  /**
   * Get paginated list of users (Admin only)
   */
  getUsers(params?: UserQueryParams): Observable<ApiResponse<PaginatedUserResponse>> {
    const queryParams = new URLSearchParams();

    if (params?.page) queryParams.append('page', params.page.toString());
    if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    if (params?.search) queryParams.append('search', params.search);
    if (params?.isActive !== undefined) queryParams.append('isActive', params.isActive.toString());
    if (params?.emailConfirmed !== undefined) queryParams.append('emailConfirmed', params.emailConfirmed.toString());

    const queryString = queryParams.toString();
    const url = queryString ? `/users?${queryString}` : '/users';

    return this.apiService.get<ApiResponse<PaginatedUserResponse>>(url);
  }

  /**
   * Get user details by ID (Admin only)
   */
  getUserById(userId: string): Observable<ApiResponse<UserDetails>> {
    return this.apiService.get<ApiResponse<UserDetails>>(`/users/${userId}`);
  }

  /**
   * Update user (Admin only)
   */
  updateUser(userId: string, updateData: UpdateUserRequest): Observable<ApiResponse<UserDetails>> {
    return this.apiService.put<ApiResponse<UserDetails>>(`/users/${userId}`, updateData);
  }

  /**
   * Deactivate user (Admin only)
   */
  deactivateUser(userId: string): Observable<ApiResponse<any>> {
    return this.apiService.post<ApiResponse<any>>(`/users/${userId}/deactivate`, {});
  }

  /**
   * Assign role to user (Admin only)
   */
  assignRole(request: AssignRoleRequest): Observable<ApiResponse<any>> {
    return this.apiService.post<ApiResponse<any>>('/users/roles/assign', request);
  }

  /**
   * Remove role from user (Admin only)
   */
  removeRole(request: AssignRoleRequest): Observable<ApiResponse<any>> {
    return this.apiService.post<ApiResponse<any>>('/users/roles/remove', request);
  }

  /**
   * Get user roles
   */
  getUserRoles(userId: string): Observable<ApiResponse<RoleInfo[]>> {
    return this.apiService.get<ApiResponse<RoleInfo[]>>(`/users/${userId}/roles`);
  }

  /**
   * Create new user (Admin only) - Optional feature
   */
  createUser(userData: CreateUserRequest): Observable<ApiResponse<UserDetails>> {
    return this.apiService.post<ApiResponse<UserDetails>>('/users', userData);
  }

  /**
   * Get available roles for assignment
   */
  getAvailableRoles(): Observable<ApiResponse<RoleInfo[]>> {
    return this.apiService.get<ApiResponse<RoleInfo[]>>('/roles');
  }
}
