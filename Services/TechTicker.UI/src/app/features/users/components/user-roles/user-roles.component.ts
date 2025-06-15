import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalService } from 'ng-zorro-antd/modal';
import { finalize } from 'rxjs/operators';
import { UserManagementService, UserDetails, RoleInfo, AssignRoleRequest } from '../../services/user-management.service';

@Component({
  selector: 'app-user-roles',
  templateUrl: './user-roles.component.html',
  styleUrls: ['./user-roles.component.css'],
  standalone: false
})
export class UserRolesComponent implements OnInit {
  user: UserDetails | null = null;
  userRoles: RoleInfo[] = [];
  availableRoles: RoleInfo[] = [];
  loading = false;
  rolesLoading = false;
  availableRolesLoading = false;
  assigningRole = false;
  removingRole = false;
  userId!: string;

  // Role assignment
  selectedRoleForAssignment: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userManagementService: UserManagementService,
    private message: NzMessageService,
    private modal: NzModalService
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.userId = params['id'];
      if (this.userId) {
        this.loadUserDetails();
        this.loadUserRoles();
        this.loadAvailableRoles();
      }
    });
  }

  loadUserDetails(): void {
    this.loading = true;

    this.userManagementService.getUserById(this.userId)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.user = response.data;
          } else {
            this.message.error('Failed to load user details');
            this.goBack();
          }
        },
        error: (error) => {
          console.error('Error loading user details:', error);
          this.message.error('Error loading user details');
          this.goBack();
        }
      });
  }

  loadUserRoles(): void {
    this.rolesLoading = true;

    this.userManagementService.getUserRoles(this.userId)
      .pipe(finalize(() => this.rolesLoading = false))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.userRoles = response.data;
          }
        },
        error: (error) => {
          console.error('Error loading user roles:', error);
          this.message.error('Error loading user roles');
        }
      });
  }

  loadAvailableRoles(): void {
    this.availableRolesLoading = true;

    this.userManagementService.getAvailableRoles()
      .pipe(finalize(() => this.availableRolesLoading = false))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.availableRoles = response.data;
          }
        },
        error: (error) => {
          console.error('Error loading available roles:', error);
          // Don't show error message as this is not critical
        }
      });
  }

  getUnassignedRoles(): RoleInfo[] {
    if (!this.user || !this.availableRoles) return [];

    const userRoleNames = this.user.roles || [];
    return this.availableRoles.filter(role => !userRoleNames.includes(role.name));
  }

  assignRole(): void {
    if (!this.selectedRoleForAssignment) {
      this.message.warning('Please select a role to assign');
      return;
    }

    this.assigningRole = true;

    const request: AssignRoleRequest = {
      userId: this.userId,
      roleName: this.selectedRoleForAssignment
    };

    this.userManagementService.assignRole(request)
      .pipe(finalize(() => this.assigningRole = false))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.message.success('Role assigned successfully');
            this.selectedRoleForAssignment = null;
            this.loadUserDetails();
            this.loadUserRoles();
          } else {
            this.message.error('Failed to assign role');
          }
        },
        error: (error) => {
          console.error('Error assigning role:', error);
          this.message.error('Error assigning role');
        }
      });
  }

  removeRole(roleName: string): void {
    this.modal.confirm({
      nzTitle: 'Remove Role',
      nzContent: `Are you sure you want to remove the "${roleName}" role from ${this.getFullName()}?`,
      nzOkText: 'Remove',
      nzOkType: 'primary',
      nzOkDanger: true,
      nzCancelText: 'Cancel',
      nzOnOk: () => {
        this.performRoleRemoval(roleName);
      }
    });
  }

  private performRoleRemoval(roleName: string): void {
    this.removingRole = true;

    const request: AssignRoleRequest = {
      userId: this.userId,
      roleName: roleName
    };

    this.userManagementService.removeRole(request)
      .pipe(finalize(() => this.removingRole = false))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.message.success('Role removed successfully');
            this.loadUserDetails();
            this.loadUserRoles();
          } else {
            this.message.error('Failed to remove role');
          }
        },
        error: (error) => {
          console.error('Error removing role:', error);
          this.message.error('Error removing role');
        }
      });
  }

  goBack(): void {
    this.router.navigate(['/users', this.userId]);
  }

  goToUsersList(): void {
    this.router.navigate(['/users']);
  }

  getFullName(): string {
    if (!this.user) return '';

    const firstName = this.user.firstName || '';
    const lastName = this.user.lastName || '';

    if (firstName && lastName) {
      return `${firstName} ${lastName}`;
    }

    return firstName || lastName || this.user.email;
  }

  getRoleDescription(roleName: string): string {
    const role = this.availableRoles.find(r => r.name === roleName);
    return role?.description || 'No description available';
  }

  getRolePermissions(roleName: string): string[] {
    const role = this.userRoles.find(r => r.name === roleName);
    return role?.permissions || [];
  }

  hasRolePermissions(roleName: string): boolean {
    return this.getRolePermissions(roleName).length > 0;
  }

  isSystemRole(roleName: string): boolean {
    // Add logic to identify system roles that shouldn't be removed
    const systemRoles = ['Admin', 'SuperAdmin'];
    return systemRoles.includes(roleName);
  }

  canRemoveRole(roleName: string): boolean {
    // Prevent removing the last admin role, etc.
    if (roleName === 'Admin' && (this.user?.roles?.filter(r => r === 'Admin').length ?? 0) === 1) {
      // Additional logic to check if this is the last admin in the system
      // For now, allow removal
    }
    return true;
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }
}
