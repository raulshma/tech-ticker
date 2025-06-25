import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { Observable } from 'rxjs';
import { AuthService, CurrentUser } from '../../services/auth.service';
import { RoleService, RoleDefinition } from '../../services/role.service';
import { HasRoleDirective } from '../../directives/has-role.directive';
import { HasAnyRoleDirective } from '../../directives/has-any-role.directive';
import { HasAllRolesDirective } from '../../directives/has-all-roles.directive';
import { HasPermissionDirective } from '../../directives/has-permission.directive';

@Component({
  selector: 'app-rbac-demo',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatChipsModule,
    MatIconModule,
    HasRoleDirective,
    HasAnyRoleDirective,
    HasAllRolesDirective,
    HasPermissionDirective
  ],
  template: `
    <div class="rbac-demo-container">
      <mat-card class="demo-card">
        <mat-card-header>
          <mat-card-title>RBAC System Demo</mat-card-title>
          <mat-card-subtitle>Role-Based Access Control Features</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <!-- Current User Info -->
          <div class="user-info" *ngIf="currentUser | async as user">
            <h3>Current User</h3>
            <p><strong>Email:</strong> {{ user.email }}</p>
            <p><strong>Name:</strong> {{ user.firstName }} {{ user.lastName }}</p>
            <div class="roles-chips">
              <mat-chip-set>
                <mat-chip *ngFor="let role of user.roles">{{ role }}</mat-chip>
              </mat-chip-set>
            </div>
          </div>

          <!-- Role-based visibility examples -->
          <div class="demo-section">
            <h3>Role-Based Visibility</h3>

            <div class="demo-item" *hasRole="'Admin'">
              <mat-icon color="primary">admin_panel_settings</mat-icon>
              <span>This is visible only to Admins</span>
            </div>

            <div class="demo-item" *hasRole="'User'">
              <mat-icon color="accent">person</mat-icon>
              <span>This is visible only to Users</span>
            </div>

            <div class="demo-item" *hasAnyRole="['User', 'Admin']">
              <mat-icon color="warn">group</mat-icon>
              <span>This is visible to Users OR Admins</span>
            </div>

            <div class="demo-item" *hasAllRoles="['User', 'Admin']">
              <mat-icon>verified_user</mat-icon>
              <span>This is visible only if user has BOTH User AND Admin roles</span>
            </div>

            <div class="demo-item" *hasRole="'Moderator'">
              <mat-icon color="primary">shield</mat-icon>
              <span>This is visible only to Moderators</span>
            </div>
          </div>

          <!-- Permission-based visibility examples -->
          <div class="demo-section">
            <h3>Permission-Based Visibility</h3>

            <div class="demo-item" *hasPermission="'products.write'">
              <mat-icon color="primary">edit</mat-icon>
              <span>Can write products (permission: products.write)</span>
            </div>

            <div class="demo-item" *hasPermission="'users.read'">
              <mat-icon color="accent">visibility</mat-icon>
              <span>Can read users (permission: users.read)</span>
            </div>

            <div class="demo-item" *hasPermission="['alerts.read', 'alerts.write']">
              <mat-icon color="warn">notifications</mat-icon>
              <span>Can read OR write alerts</span>
            </div>

            <div class="demo-item" *hasPermission="'products.write'">
              <mat-icon>inventory</mat-icon>
              <span>Can write products (permission: products.write)</span>
            </div>
          </div>

          <!-- Action buttons with role-based access -->
          <div class="demo-section">
            <h3>Role-Based Actions</h3>

            <button mat-raised-button color="primary" *hasRole="'Admin'">
              <mat-icon>settings</mat-icon>
              Admin Settings
            </button>

            <button mat-raised-button color="accent" *hasAnyRole="['User', 'Admin']">
              <mat-icon>dashboard</mat-icon>
              User Dashboard
            </button>

            <button mat-raised-button color="warn" *hasPermission="'users.write'">
              <mat-icon>person_add</mat-icon>
              Create User
            </button>
          </div>

          <!-- Role information -->
          <div class="demo-section">
            <h3>Available Roles</h3>
            <div class="roles-info">
              <mat-card *ngFor="let role of availableRoles" class="role-card">
                <mat-card-header>
                  <mat-card-title>{{ role.displayName }}</mat-card-title>
                </mat-card-header>
                <mat-card-content>
                  <p>{{ role.description }}</p>
                  <div class="permissions">
                    <strong>Permissions:</strong>
                    <mat-chip-set>
                      <mat-chip *ngFor="let permission of role.permissions">{{ permission }}</mat-chip>
                    </mat-chip-set>
                  </div>
                </mat-card-content>
              </mat-card>
            </div>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .rbac-demo-container {
      padding: 20px;
      max-width: 1200px;
      margin: 0 auto;
    }

    .demo-card {
      margin-bottom: 20px;
    }

    .user-info {
      background: #f5f5f5;
      padding: 16px;
      border-radius: 8px;
      margin-bottom: 20px;
    }

    .roles-chips {
      margin-top: 8px;
    }

    .demo-section {
      margin: 24px 0;
      padding: 16px;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
    }

    .demo-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 8px 0;
      border-bottom: 1px solid #f0f0f0;
    }

    .demo-item:last-child {
      border-bottom: none;
    }

    .roles-info {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 16px;
      margin-top: 16px;
    }

    .role-card {
      background: #fafafa;
    }

    .permissions {
      margin-top: 12px;
    }

    button {
      margin: 8px 8px 8px 0;
    }
  `]
})
export class RbacDemoComponent implements OnInit {
  currentUser: Observable<CurrentUser | null>;
  availableRoles: RoleDefinition[];

  constructor(
    private authService: AuthService,
    private roleService: RoleService
  ) {
    this.currentUser = this.authService.currentUser$;
    this.availableRoles = this.roleService.getAvailableRoles();
  }

  ngOnInit(): void {
    // Component initialization
  }
}
