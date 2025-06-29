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
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';

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
    MatMenuModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatDividerModule,
    HasPermissionDirective
  ]
})
export class UsersListComponent implements OnInit, OnDestroy {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  displayedColumns: string[] = ['email', 'name', 'roles', 'status', 'createdAt', 'actions'];
  dataSource = new MatTableDataSource<UserDto>([]);
  users: UserDto[] = [];
  isLoading = false;
  searchControl = new FormControl('');
  totalItems = 0;
  currentPage = 1;
  pageSize = 10;

  private destroy$ = new Subject<void>();

  constructor(
    private usersService: UsersService,
    private router: Router,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadUsers();
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

  setupSearch(): void {
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
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading users:', error);
        this.snackBar.open('Failed to load users', 'Close', { duration: 5000 });
        this.isLoading = false;
      }
    });
  }

  applyFilter(): void {
    const filterValue = this.searchControl.value?.toLowerCase() || '';
    this.dataSource.filter = filterValue.trim().toLowerCase();

    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.applyFilter();
  }

  createUser(): void {
    this.router.navigate(['/users/create']);
  }

  editUser(user: UserDto): void {
    this.router.navigate(['/users/edit', user.userId]);
  }

  toggleUserStatus(user: UserDto): void {
    const updateDto = new UpdateUserDto({
      isActive: !user.isActive
    });

    this.usersService.updateUser(user.userId!, updateDto).subscribe({
      next: () => {
        user.isActive = !user.isActive;
        this.snackBar.open(`User ${user.isActive ? 'activated' : 'deactivated'} successfully`, 'Close', { duration: 3000 });
      },
      error: (error) => {
        console.error('Error updating user status:', error);
        this.snackBar.open('Failed to update user status', 'Close', { duration: 5000 });
      }
    });
  }

  deleteUser(user: UserDto): void {
    const dialogRef = this.dialog.open(UserDeleteDialogComponent, {
      width: '500px',
      data: user
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.usersService.deleteUser(user.userId!).subscribe({
          next: () => {
            this.users = this.users.filter(u => u.userId !== user.userId);
            this.dataSource.data = this.users;
            this.snackBar.open('User deleted successfully', 'Close', { duration: 3000 });
          },
          error: (error) => {
            console.error('Error deleting user:', error);
            this.snackBar.open('Failed to delete user', 'Close', { duration: 5000 });
          }
        });
      }
    });
  }

  getUserDisplayName(user: UserDto): string {
    if (user.firstName && user.lastName) {
      return `${user.firstName} ${user.lastName}`;
    } else if (user.firstName) {
      return user.firstName;
    } else if (user.lastName) {
      return user.lastName;
    } else {
      return 'No name provided';
    }
  }

  getRoleColor(role: string): string {
    switch (role.toLowerCase()) {
      case 'admin':
        return 'warn';
      case 'moderator':
        return 'accent';
      case 'user':
        return 'primary';
      default:
        return 'primary';
    }
  }

  getRoleDisplayName(role: string): string {
    const displayNames = this.usersService.getRoleDisplayNames();
    return displayNames[role] || role;
  }

  get filteredUsers(): UserDto[] {
    return this.users;
  }
}
