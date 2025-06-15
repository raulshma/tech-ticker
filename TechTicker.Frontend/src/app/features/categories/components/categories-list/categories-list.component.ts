import { Component, OnInit, ViewChild } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { CategoryDto } from '../../../../shared/api/api-client';
import { CategoriesService } from '../../services/categories.service';
import { CategoryDeleteDialogComponent } from '../category-delete-dialog/category-delete-dialog.component';

@Component({
  selector: 'app-categories-list',
  templateUrl: './categories-list.component.html',
  styleUrls: ['./categories-list.component.scss'],
  standalone: false
})
export class CategoriesListComponent implements OnInit {
  displayedColumns: string[] = ['name', 'slug', 'description', 'createdAt', 'actions'];
  dataSource = new MatTableDataSource<CategoryDto>();
  isLoading = false;

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private categoriesService: CategoriesService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }

  loadCategories(): void {
    this.isLoading = true;
    this.categoriesService.getCategories().subscribe({
      next: (categories) => {
        this.dataSource.data = categories;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading categories:', error);
        this.snackBar.open('Failed to load categories', 'Close', { duration: 5000 });
        this.isLoading = false;
      }
    });
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();

    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  createCategory(): void {
    this.router.navigate(['/categories/new']);
  }

  editCategory(category: CategoryDto): void {
    this.router.navigate(['/categories/edit', category.categoryId]);
  }

  deleteCategory(category: CategoryDto): void {
    const dialogRef = this.dialog.open(CategoryDeleteDialogComponent, {
      width: '400px',
      data: category
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.isLoading = true;
        this.categoriesService.deleteCategory(category.categoryId!).subscribe({
          next: () => {
            this.snackBar.open('Category deleted successfully', 'Close', { duration: 3000 });
            this.loadCategories();
          },
          error: (error) => {
            console.error('Error deleting category:', error);
            this.snackBar.open('Failed to delete category', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      }
    });
  }
}
