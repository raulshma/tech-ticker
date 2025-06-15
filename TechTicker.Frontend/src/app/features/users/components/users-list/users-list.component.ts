import { Component, OnInit, ViewChild } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { FormControl } from '@angular/forms';
import { debounceTime, distinctUntilChanged, startWith } from 'rxjs';
import { UserDto } from '../../../../shared/api/api-client';
import { UsersService, UsersListResult } from '../../services/users.service';
import { UserDeleteDialogComponent } from '../user-delete-dialog/user-delete-dialog.component';

@Component({
  selector: 'app-users-list',
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss'],
  standalone: false
})
export class UsersListComponent implements OnInit {
  displayedColumns: string[] = ['fullName', 'email', 'roles', 'isActive', 'createdAt', 'actions'];
  dataSource = new MatTableDataSource<UserDto>();
  isLoading = false;

  // Pagination
  totalCount = 0;
  pageSize = 10;
  pageIndex = 0;
  pageSizeOptions = [5, 10, 20, 50];

  // Filters
  searchControl = new FormControl('');

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private usersService: UsersService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.setupFilters();
    this.loadUsers();
  }

  ngAfterViewInit(): void {
    this.dataSource.sort = this.sort;
  }

  setupFilters(): void {
    // Search filter
    this.searchControl.valueChanges
      .pipe(
        startWith(''),
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.applyLocalFilter();
      });
  }

  loadUsers(): void {
    this.isLoading = true;
    
    this.usersService.getUsers({
      page: this.pageIndex + 1, // API uses 1-based pagination
      pageSize: this.pageSize
    }).subscribe({
      next: (result: UsersListResult) => {
        this.dataSource.data = result.items;
        this.totalCount = result.totalCount;
        this.isLoading = false;
        this.applyLocalFilter();
      },
      error: (error) => {
        console.error('Error loading users:', error);
        this.snackBar.open('Failed to load users', 'Close', { duration: 5000 });
        this.isLoading = false;
      }
    });
  }

  applyLocalFilter(): void {
    const filterValue = this.searchControl.value?.toLowerCase() || '';
    this.dataSource.filter = filterValue;
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadUsers();
  }

  clearFilters(): void {
    this.searchControl.setValue('');
  }

  createUser(): void {
    this.router.navigate(['/users/new']);
  }

  editUser(user: UserDto): void {
    this.router.navigate(['/users/edit', user.userId]);
  }

  deleteUser(user: UserDto): void {
    const dialogRef = this.dialog.open(UserDeleteDialogComponent, {
      width: '400px',
      data: user
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.isLoading = true;
        this.usersService.deleteUser(user.userId!).subscribe({
          next: () => {
            this.snackBar.open('User deleted successfully', 'Close', { duration: 3000 });
            this.loadUsers();
          },
          error: (error) => {
            console.error('Error deleting user:', error);
            this.snackBar.open('Failed to delete user', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      }
    });
  }

  toggleUserStatus(user: UserDto): void {
    // This would typically call an API endpoint to activate/deactivate the user
    // For now, we'll just show a message
    const action = user.isActive ? 'deactivate' : 'activate';
    this.snackBar.open(`Feature to ${action} user will be implemented`, 'Close', { duration: 3000 });
  }

  getRolesDisplay(roles: string[] | undefined): string {
    if (!roles || roles.length === 0) {
      return 'No roles';
    }
    return roles.join(', ');
  }

  getStatusColor(isActive: boolean | undefined): string {
    return isActive ? 'status-active' : 'status-inactive';
  }

  getStatusText(isActive: boolean | undefined): string {
    return isActive ? 'Active' : 'Inactive';
  }
}
