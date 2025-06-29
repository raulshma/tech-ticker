import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { Router } from '@angular/router';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';
import { UserDto, UpdateUserDto } from '../../../../shared/api/api-client';
import { UsersService } from '../../services/users.service';
import { UserDeleteDialogComponent } from '../user-delete-dialog/user-delete-dialog.component';
import { RbacModule } from '../../../../shared/modules/rbac.module';

@Component({
  selector: 'app-users-list',
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatMenuModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatDividerModule,
    RbacModule
  ]
})
export class UsersListComponent implements OnInit, OnDestroy {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  displayedColumns: string[] = ['user', 'roles', 'status', 'lastActivity', 'actions'];
  dataSource = new MatTableDataSource<UserDto>([]);
  users: UserDto[] = [];
  isLoading = false;
  searchControl = new FormControl('');
  totalItems = 0;
  currentPage = 1;
  pageSize = 10;

  // Enhanced filtering
  statusFilter = '';
  roleFilter = '';
  availableRoles: string[] = [];

  private destroy$ = new Subject<void>();

  constructor(
    private usersService: UsersService,
    private router: Router,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadUsers();
    this.loadAvailableRoles();
    this.setupSearch();
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupSearch(): void {
    this.searchControl.valueChanges
      .pipe(
        takeUntil(this.destroy$),
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.applyFilter();
      });
  }

  loadUsers(): void {
    this.isLoading = true;
    this.usersService.getUsers({
      page: this.currentPage,
      pageSize: this.pageSize
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (result) => {
        this.users = result.items;
        this.totalItems = result.totalCount;
        this.dataSource.data = this.users;
        this.applyFilter();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading users:', error);
        this.snackBar.open('Failed to load users', 'Close', { duration: 5000 });
        this.isLoading = false;
      }
    });
  }

  private loadAvailableRoles(): void {
    // Extract available roles from users service
    const roleDisplayNames = this.usersService.getRoleDisplayNames();
    this.availableRoles = Object.keys(roleDisplayNames);
  }

  private applyFilter(): void {
    const searchValue = this.searchControl.value?.toLowerCase() || '';
    
    this.dataSource.filter = JSON.stringify({
      search: searchValue,
      status: this.statusFilter,
      role: this.roleFilter
    });

    this.dataSource.filterPredicate = (user: UserDto, filter: string): boolean => {
      const filterObject = JSON.parse(filter);
      const searchMatch = !filterObject.search || 
        (user.email?.toLowerCase().includes(filterObject.search) || false) ||
        (user.firstName?.toLowerCase().includes(filterObject.search) || false) ||
        (user.lastName?.toLowerCase().includes(filterObject.search) || false) ||
        (user.roles?.some(role => role.toLowerCase().includes(filterObject.search)) || false);

      const statusMatch = !filterObject.status || 
        (filterObject.status === 'active' && user.isActive === true) ||
        (filterObject.status === 'inactive' && user.isActive === false);

      const roleMatch = !filterObject.role || 
        (user.roles?.includes(filterObject.role) || false);

      return searchMatch && statusMatch && roleMatch;
    };
  }

  onFilterChange(): void {
    this.applyFilter();
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.statusFilter = '';
    this.roleFilter = '';
    this.applyFilter();
  }

  createUser(): void {
    this.router.navigate(['/users/new']);
  }

  editUser(user: UserDto): void {
    this.router.navigate(['/users/edit', user.userId]);
  }

  managePermissions(user: UserDto): void {
    this.router.navigate(['/users/edit', user.userId], { fragment: 'permissions' });
  }

  toggleUserStatus(user: UserDto): void {
    const newStatus = !user.isActive;
    const updateData = new UpdateUserDto({
      isActive: newStatus
    });

    this.usersService.updateUser(user.userId!, updateData)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          user.isActive = newStatus;
          this.snackBar.open(
            `User ${newStatus ? 'activated' : 'deactivated'} successfully`,
            'Close',
            { duration: 3000 }
          );
        },
        error: (error) => {
          console.error('Error updating user status:', error);
          this.snackBar.open('Failed to update user status', 'Close', { duration: 5000 });
        }
      });
  }

  deleteUser(user: UserDto): void {
    const dialogRef = this.dialog.open(UserDeleteDialogComponent, {
      data: { user },
      width: '400px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.performUserDeletion(user);
      }
    });
  }

  private performUserDeletion(user: UserDto): void {
    this.usersService.deleteUser(user.userId!)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.users = this.users.filter(u => u.userId !== user.userId);
          this.dataSource.data = this.users;
          this.totalItems--;
          this.snackBar.open('User deleted successfully', 'Close', { duration: 3000 });
        },
        error: (error) => {
          console.error('Error deleting user:', error);
          this.snackBar.open('Failed to delete user', 'Close', { duration: 5000 });
        }
      });
  }

  // Statistics methods
  getActiveUsersCount(): number {
    return this.users.filter(user => user.isActive).length;
  }

  getAdminUsersCount(): number {
    return this.users.filter(user => 
      user.roles?.some(role => role.toLowerCase().includes('admin'))
    ).length;
  }

  getRecentSignupsCount(): number {
    const thirtyDaysAgo = new Date();
    thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);
    
    return this.users.filter(user => {
      const createdDate = new Date(user.createdAt!);
      return createdDate >= thirtyDaysAgo;
    }).length;
  }

  // Display helper methods
  getUserDisplayName(user: UserDto): string {
    if (user.firstName && user.lastName) {
      return `${user.firstName} ${user.lastName}`;
    } else if (user.firstName) {
      return user.firstName;
    } else if (user.lastName) {
      return user.lastName;
    } else {
      return 'Unnamed User';
    }
  }

  getRoleColor(role: string): string {
    const colorMap: { [key: string]: string } = {
      'Admin': 'primary',
      'Manager': 'accent',
      'User': 'basic',
      'Moderator': 'warn'
    };
    return colorMap[role] || 'basic';
  }

  getRoleIcon(role: string): string {
    const iconMap: { [key: string]: string } = {
      'Admin': 'admin_panel_settings',
      'Manager': 'supervisor_account',
      'User': 'person',
      'Moderator': 'shield'
    };
    return iconMap[role] || 'person';
  }

  getRoleDisplayName(role: string): string {
    const displayNames = this.usersService.getRoleDisplayNames();
    return displayNames[role] || role;
  }

  formatDate(date: string | Date): string {
    if (!date) return 'Never';
    const dateObj = new Date(date);
    return dateObj.toLocaleDateString();
  }

  formatTime(date: string | Date): string {
    if (!date) return '';
    const dateObj = new Date(date);
    return dateObj.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  get filteredUsers(): UserDto[] {
    return this.dataSource.filteredData;
  }
}
