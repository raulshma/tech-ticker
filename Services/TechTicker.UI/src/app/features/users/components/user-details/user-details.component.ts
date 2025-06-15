import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { finalize } from 'rxjs/operators';
import { UserManagementService, UserDetails, RoleInfo } from '../../services/user-management.service';

@Component({
  selector: 'app-user-details',
  templateUrl: './user-details.component.html',
  styleUrls: ['./user-details.component.css']
})
export class UserDetailsComponent implements OnInit {
  user: UserDetails | null = null;
  userRoles: RoleInfo[] = [];
  loading = false;
  rolesLoading = false;
  userId!: string;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userManagementService: UserManagementService,
    private message: NzMessageService
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.userId = params['id'];
      if (this.userId) {
        this.loadUserDetails();
        this.loadUserRoles();
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
            this.router.navigate(['/users']);
          }
        },
        error: (error) => {
          console.error('Error loading user details:', error);
          this.message.error('Error loading user details');
          this.router.navigate(['/users']);
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
          // Don't show error message for roles as it's not critical
        }
      });
  }

  editUser(): void {
    this.router.navigate(['/users', this.userId, 'edit']);
  }

  manageRoles(): void {
    this.router.navigate(['/users', this.userId, 'roles']);
  }

  goBack(): void {
    this.router.navigate(['/users']);
  }

  getStatusColor(isActive: boolean): string {
    return isActive ? 'green' : 'red';
  }

  getStatusText(isActive: boolean): string {
    return isActive ? 'Active' : 'Inactive';
  }

  getEmailStatusColor(emailConfirmed: boolean): string {
    return emailConfirmed ? 'green' : 'orange';
  }

  getEmailStatusText(emailConfirmed: boolean): string {
    return emailConfirmed ? 'Verified' : 'Pending Verification';
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString();
  }

  formatDateOnly(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }

  getFullName(): string {
    if (!this.user) return '';

    const firstName = this.user.firstName || '';
    const lastName = this.user.lastName || '';

    if (firstName && lastName) {
      return `${firstName} ${lastName}`;
    }

    return firstName || lastName || 'No name provided';
  }

  getRoleNames(): string {
    return this.user?.roles.join(', ') || 'No roles assigned';
  }

  getRoleDescriptions(): string[] {
    return this.userRoles.map(role => role.description || 'No description').filter(desc => desc !== 'No description');
  }

  hasPermissions(): boolean {
    return this.userRoles.some(role => role.permissions && role.permissions.length > 0);
  }

  getAllPermissions(): string[] {
    const permissions: string[] = [];
    this.userRoles.forEach(role => {
      if (role.permissions) {
        permissions.push(...role.permissions);
      }
    });
    return [...new Set(permissions)]; // Remove duplicates
  }
}
