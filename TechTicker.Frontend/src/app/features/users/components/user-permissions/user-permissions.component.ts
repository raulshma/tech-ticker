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
  templateUrl: './user-permissions.component.html',
  styleUrls: ['./user-permissions.component.scss']
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

  getCategoryIcon(categoryName: string): string {
    const iconMap: { [key: string]: string } = {
      'Users': 'people',
      'Products': 'inventory',
      'Categories': 'category',
      'Alerts': 'notifications',
      'System': 'settings',
      'Reports': 'assessment',
      'Admin': 'admin_panel_settings',
      'Permissions': 'security',
      'Default': 'folder'
    };
    
    return iconMap[categoryName] || iconMap['Default'];
  }
} 