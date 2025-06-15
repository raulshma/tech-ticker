import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalService } from 'ng-zorro-antd/modal';
import { finalize } from 'rxjs/operators';
import { UserManagementService, UserListItem, UserQueryParams } from '../../services/user-management.service';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.css']
})
export class UserListComponent implements OnInit {
  users: UserListItem[] = [];
  loading = false;
  searchValue = '';

  // Pagination
  totalCount = 0;
  currentPage = 1;
  pageSize = 10;
  pageSizeOptions = [10, 20, 50, 100];

  // Filters
  activeFilter?: boolean;
  emailConfirmedFilter?: boolean;

  // Table sorting
  sortField: string | null = null;
  sortOrder: string | null = null;

  constructor(
    private userManagementService: UserManagementService,
    private router: Router,
    private message: NzMessageService,
    private modal: NzModalService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;

    const params: UserQueryParams = {
      page: this.currentPage,
      pageSize: this.pageSize,
      search: this.searchValue || undefined,
      isActive: this.activeFilter,
      emailConfirmed: this.emailConfirmedFilter
    };

    this.userManagementService.getUsers(params)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.users = response.data.items;
            this.totalCount = response.data.totalCount;
          } else {
            this.message.error('Failed to load users');
          }
        },
        error: (error) => {
          console.error('Error loading users:', error);
          this.message.error('Error loading users');
        }
      });
  }

  onSearchChange(): void {
    this.currentPage = 1;
    this.loadUsers();
  }

  onSearch(): void {
    this.onSearchChange();
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadUsers();
  }

  onPageIndexChange(page: number): void {
    this.currentPage = page;
    this.loadUsers();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.currentPage = 1;
    this.loadUsers();
  }

  onSort(sort: { key: string; value: string | null }): void {
    this.sortField = sort.key;
    this.sortOrder = sort.value;
    // Note: Implement server-side sorting if needed
    this.loadUsers();
  }

  viewUser(userId: string): void {
    this.router.navigate(['/users', userId]);
  }

  editUser(userId: string): void {
    this.router.navigate(['/users', userId, 'edit']);
  }

  manageRoles(userId: string): void {
    this.router.navigate(['/users', userId, 'roles']);
  }

  changeStatus(user: UserListItem): void {
    const action = user.isActive ? 'deactivate' : 'activate';
    const actionText = user.isActive ? 'Deactivate' : 'Activate';

    this.modal.confirm({
      nzTitle: `${actionText} User`,
      nzContent: `Are you sure you want to ${action} ${user.firstName || user.email}?`,
      nzOkText: actionText,
      nzOkType: user.isActive ? 'primary' : 'default',
      nzCancelText: 'Cancel',
      nzOnOk: () => {
        if (user.isActive) {
          this.deactivateUser(user.userId);
        } else {
          // For reactivation, we would need a separate API endpoint
          this.message.info('User reactivation feature coming soon');
        }
      }
    });
  }

  private deactivateUser(userId: string): void {
    this.userManagementService.deactivateUser(userId)
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.message.success('User deactivated successfully');
            this.loadUsers();
          } else {
            this.message.error('Failed to deactivate user');
          }
        },
        error: (error) => {
          console.error('Error deactivating user:', error);
          this.message.error('Error deactivating user');
        }
      });
  }

  resetFilters(): void {
    this.searchValue = '';
    this.activeFilter = undefined;
    this.emailConfirmedFilter = undefined;
    this.currentPage = 1;
    this.loadUsers();
  }

  getStatusColor(isActive: boolean): string {
    return isActive ? 'green' : 'red';
  }

  getStatusText(isActive: boolean): string {
    return isActive ? 'Active' : 'Inactive';
  }

  getRolesDisplay(roles: string[]): string {
    return roles.length > 0 ? roles.join(', ') : 'No roles';
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }
}
