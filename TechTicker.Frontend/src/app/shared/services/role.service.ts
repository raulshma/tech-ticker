import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, map, catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { TechTickerApiClient, RoleInfoDto, RoleInfoDtoIEnumerableApiResponse } from '../api/api-client';

export interface RoleDefinition {
  name: string;
  displayName: string;
  description: string;
  permissions: string[];
}

export interface RoleHierarchy {
  [role: string]: string[];
}

@Injectable({
  providedIn: 'root'
})
export class RoleService {
  private availableRoles: RoleDefinition[] = [
    {
      name: 'Admin',
      displayName: 'Administrator',
      description: 'Full system access with all administrative privileges',
      permissions: ['*'] // All permissions
    },
    {
      name: 'Moderator',
      displayName: 'Moderator',
      description: 'Can manage content and moderate user activities',
      permissions: [
        'products.read',
        'products.write',
        'categories.read',
        'categories.write',
        'mappings.read',
        'mappings.write',
        'scraper-logs.read'
      ]
    },
    {
      name: 'User',
      displayName: 'User',
      description: 'Standard user with basic access to user features',
      permissions: [
        'dashboard.read',
        'alerts.read',
        'alerts.write',
        'profile.read',
        'profile.write'
      ]
    }
  ];

  // Role hierarchy - higher roles inherit permissions from lower roles
  private roleHierarchy: RoleHierarchy = {
    'Admin': ['Moderator', 'User'],
    'Moderator': ['User'],
    'User': []
  };

  constructor(
    private authService: AuthService,
    private apiClient: TechTickerApiClient
  ) {}

  getAvailableRoles(): RoleDefinition[] {
    return this.availableRoles;
  }

  // Fetch roles from API (for admin use)
  fetchRolesFromApi(): Observable<RoleInfoDto[]> {
    return this.apiClient.getAllRoles()
      .pipe(
        map((response: RoleInfoDtoIEnumerableApiResponse) => {
          if (!response.success || !response.data) {
            throw new Error(response.message || 'Failed to fetch roles');
          }
          return response.data;
        }),
        catchError(error => {
          console.error('Error fetching roles:', error);
          return throwError(() => error);
        })
      );
  }

  getRoleDefinition(roleName: string): RoleDefinition | undefined {
    return this.availableRoles.find(role => role.name === roleName);
  }

  getRoleDisplayName(roleName: string): string {
    const role = this.getRoleDefinition(roleName);
    return role?.displayName || roleName;
  }

  getRoleDescription(roleName: string): string {
    const role = this.getRoleDefinition(roleName);
    return role?.description || '';
  }

  hasPermission(permission: string): boolean {
    const userRoles = this.authService.getCurrentUserRoles();

    // Check if user has admin role (has all permissions)
    if (userRoles.includes('Admin')) {
      return true;
    }

    // Check permissions for each user role (including inherited roles)
    for (const userRole of userRoles) {
      const allRoles = this.getAllInheritedRoles(userRole);

      for (const role of allRoles) {
        const roleDefinition = this.getRoleDefinition(role);
        if (roleDefinition?.permissions.includes(permission) ||
            roleDefinition?.permissions.includes('*')) {
          return true;
        }
      }
    }

    return false;
  }

  hasAnyPermission(permissions: string[]): boolean {
    return permissions.some(permission => this.hasPermission(permission));
  }

  hasAllPermissions(permissions: string[]): boolean {
    return permissions.every(permission => this.hasPermission(permission));
  }

  private getAllInheritedRoles(roleName: string): string[] {
    const inherited = [roleName];
    const childRoles = this.roleHierarchy[roleName] || [];

    for (const childRole of childRoles) {
      inherited.push(...this.getAllInheritedRoles(childRole));
    }

    return [...new Set(inherited)]; // Remove duplicates
  }

  canAccessFeature(feature: string): boolean {
    const featurePermissions: { [key: string]: string[] } = {
      'categories': ['categories.read'],
      'products': ['products.read'],
      'mappings': ['mappings.read'],
      'site-configs': ['site-configs.read'],
      'users': ['users.read'],
      'scraper-logs': ['scraper-logs.read'],
      'alerts': ['alerts.read'],
      'dashboard': ['dashboard.read']
    };

    const requiredPermissions = featurePermissions[feature];
    if (!requiredPermissions) {
      return false;
    }

    return this.hasAnyPermission(requiredPermissions);
  }

  canModifyFeature(feature: string): boolean {
    const featurePermissions: { [key: string]: string[] } = {
      'categories': ['categories.write'],
      'products': ['products.write'],
      'mappings': ['mappings.write'],
      'site-configs': ['site-configs.write'],
      'users': ['users.write'],
      'alerts': ['alerts.write']
    };

    const requiredPermissions = featurePermissions[feature];
    if (!requiredPermissions) {
      return false;
    }

    return this.hasAnyPermission(requiredPermissions);
  }
}
