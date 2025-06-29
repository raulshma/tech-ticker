import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, takeUntil, forkJoin, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { UserDto, PermissionDto } from '../../../../shared/api/api-client';
import { UsersService, PermissionCategory } from '../../services/users.service';

@Component({
  selector: 'app-user-permissions',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatChipsModule,
    MatIconModule,
    MatExpansionModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  template: `
    <mat-card class="permissions-card">
      <mat-card-header>
        <mat-card-title>User Permissions</mat-card-title>
        <mat-card-subtitle>Manage permissions for {{ user?.firstName }} {{ user?.lastName }}</mat-card-subtitle>
      </mat-card-header>

      <mat-card-content>
        <!-- Loading State -->
        <div *ngIf="isLoading" class="loading-container">
          <mat-spinner diameter="40"></mat-spinner>
          <p>Loading permissions...</p>
        </div>

        <!-- Error State -->
        <div *ngIf="error" class="error-container">
          <mat-icon color="warn">error</mat-icon>
          <p>{{ error }}</p>
          <button mat-button color="primary" (click)="loadPermissions()">
            <mat-icon>refresh</mat-icon>
            Retry
          </button>
        </div>

        <!-- Permissions Content -->
        <div *ngIf="!isLoading && !error" class="permissions-content">
          <!-- Current User Permissions -->
          <div class="current-permissions">
            <h3>Current Permissions</h3>
            <div *ngIf="userPermissions.length === 0" class="no-permissions">
              <mat-icon>info</mat-icon>
              <p>No direct permissions assigned. Permissions are inherited from roles.</p>
            </div>
            <div *ngIf="userPermissions.length > 0" class="permissions-list">
              <mat-chip-set>
                <mat-chip 
                  *ngFor="let permission of userPermissions" 
                  [removable]="true"
                  (removed)="removePermission(permission)"
                  color="primary">
                  {{ permission.name }}
                  <mat-icon matChipRemove>cancel</mat-icon>
                </mat-chip>
              </mat-chip-set>
            </div>
          </div>

          <!-- Assign New Permissions -->
          <div class="assign-permissions">
            <h3>Assign Permissions</h3>
            <p class="description">Select permissions to assign directly to this user:</p>

            <mat-accordion>
              <mat-expansion-panel 
                *ngFor="let category of permissionCategories" 
                [expanded]="true">
                <mat-expansion-panel-header>
                  <mat-panel-title>
                    {{ category.name }}
                  </mat-panel-title>
                  <mat-panel-description>
                    {{ category.permissions.length }} permissions
                  </mat-panel-description>
                </mat-expansion-panel-header>

                <div class="category-permissions">
                  <div 
                    *ngFor="let permission of category.permissions" 
                    class="permission-item"
                    [class.assigned]="isPermissionAssigned(permission)">
                    <mat-checkbox
                      [checked]="isPermissionAssigned(permission)"
                      (change)="togglePermission(permission, $event.checked)"
                      [disabled]="isUpdating"
                      matTooltip="{{ permission.description || permission.name }}">
                      {{ permission.name }}
                    </mat-checkbox>
                    <span class="permission-description" *ngIf="permission.description">
                      {{ permission.description }}
                    </span>
                  </div>
                </div>
              </mat-expansion-panel>
            </mat-accordion>
          </div>

          <!-- Actions -->
          <div class="actions" *ngIf="hasChanges()">
            <button 
              mat-raised-button 
              color="primary" 
              (click)="saveChanges()"
              [disabled]="isUpdating">
              <mat-spinner diameter="16" *ngIf="isUpdating"></mat-spinner>
              <mat-icon *ngIf="!isUpdating">save</mat-icon>
              {{ isUpdating ? 'Saving...' : 'Save Changes' }}
            </button>
            <button 
              mat-button 
              (click)="resetChanges()"
              [disabled]="isUpdating">
              <mat-icon>undo</mat-icon>
              Reset
            </button>
          </div>
        </div>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .permissions-card {
      margin: 16px 0;
    }

    .loading-container,
    .error-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 32px;
      text-align: center;
    }

    .error-container {
      color: #d32f2f;
    }

    .error-container mat-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      margin-bottom: 16px;
    }

    .permissions-content {
      padding: 16px 0;
    }

    .current-permissions {
      margin-bottom: 32px;
    }

    .current-permissions h3 {
      margin-bottom: 16px;
      color: #1976d2;
    }

    .no-permissions {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 16px;
      background: #f5f5f5;
      border-radius: 8px;
      color: #666;
    }

    .permissions-list {
      margin-top: 16px;
    }

    .assign-permissions h3 {
      margin-bottom: 8px;
      color: #1976d2;
    }

    .description {
      color: #666;
      margin-bottom: 16px;
    }

    .category-permissions {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 16px;
      padding: 16px 0;
    }

    .permission-item {
      display: flex;
      flex-direction: column;
      padding: 12px;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      background: #fafafa;
      transition: all 0.2s ease;
    }

    .permission-item:hover {
      background: #f0f0f0;
      border-color: #1976d2;
    }

    .permission-item.assigned {
      background: #e3f2fd;
      border-color: #1976d2;
    }

    .permission-description {
      font-size: 12px;
      color: #666;
      margin-top: 4px;
      margin-left: 24px;
    }

    .actions {
      display: flex;
      gap: 16px;
      margin-top: 24px;
      padding-top: 16px;
      border-top: 1px solid #e0e0e0;
    }

    mat-expansion-panel {
      margin-bottom: 8px;
    }

    mat-expansion-panel-header {
      background: #f5f5f5;
    }

    mat-panel-title {
      font-weight: 500;
    }

    mat-panel-description {
      color: #666;
    }
  `]
})
export class UserPermissionsComponent implements OnInit, OnDestroy {
  @Input() user: UserDto | null = null;

  isLoading = false;
  isUpdating = false;
  error: string | null = null;

  userPermissions: PermissionDto[] = [];
  allPermissions: PermissionDto[] = [];
  permissionCategories: PermissionCategory[] = [];
  pendingChanges: Set<string> = new Set();

  private destroy$ = new Subject<void>();

  constructor(
    private usersService: UsersService
  ) {}

  ngOnInit(): void {
    if (this.user) {
      this.loadPermissions();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadPermissions(): void {
    if (!this.user?.userId) return;

    this.isLoading = true;
    this.error = null;

    forkJoin({
      userPermissions: this.usersService.getUserPermissions(this.user.userId),
      allPermissions: this.usersService.getAssignablePermissions()
    })
    .pipe(
      takeUntil(this.destroy$),
      catchError(error => {
        this.error = error.message || 'Failed to load permissions';
        return of({ userPermissions: [], allPermissions: [] });
      }),
      finalize(() => this.isLoading = false)
    )
    .subscribe({
      next: (result) => {
        this.userPermissions = result.userPermissions;
        this.allPermissions = result.allPermissions;
        this.permissionCategories = this.usersService.groupPermissionsByCategory(this.allPermissions);
      }
    });
  }

  isPermissionAssigned(permission: PermissionDto): boolean {
    return this.userPermissions.some(p => p.permissionId === permission.permissionId);
  }

  togglePermission(permission: PermissionDto, checked: boolean): void {
    const permissionId = permission.permissionId;
    
    if (checked) {
      this.pendingChanges.add(permissionId!);
    } else {
      this.pendingChanges.delete(permissionId!);
    }
  }

  removePermission(permission: PermissionDto): void {
    if (!this.user?.userId) return;

    this.isUpdating = true;
    this.usersService.removePermissionFromUser(this.user.userId, permission.permissionId!)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.isUpdating = false)
      )
      .subscribe({
        next: () => {
          this.userPermissions = this.userPermissions.filter(p => p.permissionId !== permission.permissionId);
          this.pendingChanges.delete(permission.permissionId!);
        },
        error: (error) => {
          console.error('Error removing permission:', error);
          // Handle error (could show snackbar)
        }
      });
  }

  hasChanges(): boolean {
    return this.pendingChanges.size > 0;
  }

  saveChanges(): void {
    if (!this.user?.userId || !this.hasChanges()) return;

    this.isUpdating = true;
    const permissionIds = Array.from(this.pendingChanges);

    this.usersService.bulkAssignPermissionsToUser(this.user.userId, permissionIds)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.isUpdating = false)
      )
      .subscribe({
        next: () => {
          // Add the new permissions to the user's permissions list
          const newPermissions = this.allPermissions.filter(p => 
            permissionIds.includes(p.permissionId!) && 
            !this.userPermissions.some(up => up.permissionId === p.permissionId)
          );
          this.userPermissions.push(...newPermissions);
          this.pendingChanges.clear();
        },
        error: (error) => {
          console.error('Error saving permissions:', error);
          // Handle error (could show snackbar)
        }
      });
  }

  resetChanges(): void {
    this.pendingChanges.clear();
  }
} 