# Role-Based Access Control (RBAC) System

This document describes how to use the RBAC system implemented in the TechTicker Angular frontend.

## Overview

The RBAC system provides comprehensive role and permission-based access control for the application. It includes:

- **Role-based authentication and authorization**
- **Permission-based access control**
- **Structural directives for UI visibility**
- **Route guards for navigation protection**
- **Interceptors for API error handling**

## Available Roles

### Admin
- **Display Name**: Administrator
- **Description**: Full system access with all administrative privileges
- **Permissions**: All permissions (*)

### Moderator
- **Display Name**: Moderator
- **Description**: Can manage content and moderate user activities
- **Permissions**: 
  - products.read, products.write
  - categories.read, categories.write
  - mappings.read, mappings.write
  - scraper-logs.read

### User
- **Display Name**: User
- **Description**: Standard user with basic access to user features
- **Permissions**:
  - dashboard.read
  - alerts.read, alerts.write
  - profile.read, profile.write

## Structural Directives

### *hasRole
Shows/hides elements based on user roles.

```html
<!-- Single role -->
<div *hasRole="'Admin'">Admin only content</div>

<!-- Multiple roles (OR logic) -->
<div *hasRole="['Admin', 'Moderator']">Admin or Moderator content</div>

<!-- Multiple roles (AND logic) -->
<div *hasRole="['Admin', 'User']" [hasRoleRequireAll]="true">
  Must have both Admin AND User roles
</div>
```

### *hasAnyRole
Shows/hides elements if user has ANY of the specified roles.

```html
<div *hasAnyRole="['User', 'Admin']">
  Visible to Users OR Admins
</div>
```

### *hasAllRoles
Shows/hides elements if user has ALL of the specified roles.

```html
<div *hasAllRoles="['Admin', 'Moderator']">
  Visible only if user has BOTH Admin AND Moderator roles
</div>
```

### *hasPermission
Shows/hides elements based on permissions.

```html
<!-- Single permission -->
<button *hasPermission="'products.write'">Create Product</button>

<!-- Multiple permissions (OR logic) -->
<div *hasPermission="['products.read', 'products.write']">
  Can read OR write products
</div>

<!-- Multiple permissions (AND logic) -->
<div *hasPermission="['products.read', 'products.write']" [hasPermissionRequireAll]="true">
  Can read AND write products
</div>
```

## Route Guards

### AuthGuard
Ensures user is authenticated.

```typescript
{
  path: 'protected',
  component: ProtectedComponent,
  canActivate: [AuthGuard]
}
```

### AdminGuard
Ensures user has Admin role.

```typescript
{
  path: 'admin',
  component: AdminComponent,
  canActivate: [AdminGuard]
}
```

### RoleGuard
Flexible guard that checks for specific roles.

```typescript
{
  path: 'moderator',
  component: ModeratorComponent,
  canActivate: [RoleGuard],
  data: { 
    roles: ['Admin', 'Moderator'],
    requireAll: false, // OR logic (default)
    redirectTo: '/dashboard' // Where to redirect if access denied
  }
}
```

### UserGuard
Ensures user has User or Admin role.

```typescript
{
  path: 'user-feature',
  component: UserFeatureComponent,
  canActivate: [UserGuard]
}
```

## Services

### AuthService
Enhanced with additional role checking methods:

```typescript
// Check specific role
authService.hasRole('Admin')

// Check any of multiple roles
authService.hasAnyRole(['User', 'Admin'])

// Check all of multiple roles
authService.hasAllRoles(['Admin', 'Moderator'])

// Generic access check
authService.canAccess(['User', 'Admin'], false) // OR logic
authService.canAccess(['Admin', 'Moderator'], true) // AND logic

// Get current user roles
authService.getCurrentUserRoles()
```

### RoleService
Manages role definitions and permissions:

```typescript
// Check permissions
roleService.hasPermission('products.write')
roleService.hasAnyPermission(['products.read', 'products.write'])
roleService.hasAllPermissions(['products.read', 'products.write'])

// Feature access
roleService.canAccessFeature('categories')
roleService.canModifyFeature('products')

// Role information
roleService.getAvailableRoles()
roleService.getRoleDefinition('Admin')
roleService.getRoleDisplayName('Admin')
```

## Error Handling

The `AuthorizationInterceptor` automatically handles:
- **401 Unauthorized**: Logs out user and redirects to login
- **403 Forbidden**: Shows error message and redirects to dashboard

## Usage Examples

### Component with Role-Based Features

```typescript
@Component({
  template: `
    <div *hasRole="'Admin'">
      <button (click)="deleteUser()">Delete User</button>
    </div>
    
    <div *hasAnyRole="['User', 'Admin']">
      <button (click)="editProfile()">Edit Profile</button>
    </div>
    
    <div *hasPermission="'products.write'">
      <button (click)="createProduct()">Create Product</button>
    </div>
  `
})
export class MyComponent {
  constructor(
    private authService: AuthService,
    private roleService: RoleService
  ) {}
  
  canDeleteUser(): boolean {
    return this.authService.hasRole('Admin');
  }
  
  canCreateProduct(): boolean {
    return this.roleService.hasPermission('products.write');
  }
}
```

### Navigation Menu with Role-Based Items

```html
<mat-nav-list>
  <a mat-list-item routerLink="/dashboard">Dashboard</a>
  
  <ng-container *hasRole="'Admin'">
    <a mat-list-item routerLink="/admin">Admin Panel</a>
    <a mat-list-item routerLink="/users">User Management</a>
  </ng-container>
  
  <ng-container *hasAnyRole="['User', 'Admin']">
    <a mat-list-item routerLink="/alerts">My Alerts</a>
  </ng-container>
  
  <ng-container *hasPermission="'products.write'">
    <a mat-list-item routerLink="/products/create">Create Product</a>
  </ng-container>
</mat-nav-list>
```

## Best Practices

1. **Use specific permissions** rather than roles when possible
2. **Combine guards and directives** for comprehensive protection
3. **Handle unauthorized access gracefully** with appropriate redirects
4. **Test role-based features** with different user roles
5. **Keep role definitions centralized** in the RoleService
6. **Use meaningful permission names** that describe the action

## Testing

To test RBAC features:

1. Create test users with different roles
2. Log in with each user type
3. Verify UI elements show/hide correctly
4. Test route access with different roles
5. Verify API error handling works correctly
