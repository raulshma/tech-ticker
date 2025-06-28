import { Component, OnInit, ViewChild } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { FormControl } from '@angular/forms';
import { debounceTime, distinctUntilChanged, startWith } from 'rxjs';
import { ProductDto, CategoryDto } from '../../../../shared/api/api-client';
import { ProductsService, ProductsFilter, PagedResult } from '../../services/products.service';
import { CategoriesService } from '../../../categories/services/categories.service';
import { ProductDeleteDialogComponent } from '../product-delete-dialog/product-delete-dialog.component';
import { environment } from '../../../../../environments/environment';


@Component({
  selector: 'app-products-list',
  templateUrl: './products-list.component.html',
  styleUrls: ['./products-list.component.scss'],
  standalone: false
})
export class ProductsListComponent implements OnInit {
  displayedColumns: string[] = ['image', 'name', 'category', 'manufacturer', 'modelNumber', 'isActive', 'createdAt', 'actions'];
  dataSource = new MatTableDataSource<ProductDto>();
  isLoading = false;

  // Pagination
  totalCount = 0;
  pageSize = 10;
  currentPage = 0;
  pageSizeOptions = [5, 10, 20, 50];

  // Filters
  searchControl = new FormControl('');
  categoryControl = new FormControl('');
  categories: CategoryDto[] = [];

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private productsService: ProductsService,
    private categoriesService: CategoriesService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCategories();
    this.setupFilters();
    this.loadProducts();
  }

  setupFilters(): void {
    // Search filter with debounce
    this.searchControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.currentPage = 0;
        this.loadProducts();
      });

    // Category filter
    this.categoryControl.valueChanges
      .subscribe(() => {
        this.currentPage = 0;
        this.loadProducts();
      });
  }

  loadCategories(): void {
    this.categoriesService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories;
      },
      error: (error) => {
        console.error('Error loading categories:', error);
      }
    });
  }

  loadProducts(): void {
    this.isLoading = true;

    const filter: ProductsFilter = {
      search: this.searchControl.value || undefined,
      categoryId: this.categoryControl.value || undefined,
      page: this.currentPage + 1,
      pageSize: this.pageSize
    };

    this.productsService.getProducts(filter).subscribe({
      next: (result: PagedResult<ProductDto>) => {
        this.dataSource.data = result.items;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading products:', error);
        this.snackBar.open('Failed to load products', 'Close', { duration: 5000 });
        this.isLoading = false;
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadProducts();
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.categoryControl.setValue('');
  }

  createProduct(): void {
    this.router.navigate(['/products/new']);
  }

  viewPriceHistory(product: ProductDto): void {
    this.router.navigate(['/products', product.productId, 'price-history']);
  }

  editProduct(product: ProductDto): void {
    this.router.navigate(['/products/edit', product.productId]);
  }

  deleteProduct(product: ProductDto): void {
    const dialogRef = this.dialog.open(ProductDeleteDialogComponent, {
      width: '400px',
      data: product
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.isLoading = true;
        this.productsService.deleteProduct(product.productId!).subscribe({
          next: () => {
            this.snackBar.open('Product deleted successfully', 'Close', { duration: 3000 });
            this.loadProducts();
          },
          error: (error) => {
            console.error('Error deleting product:', error);
            this.snackBar.open('Failed to delete product', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      }
    });
  }

  getCategoryName(categoryId: string): string {
    const category = this.categories.find(c => c.categoryId === categoryId);
    return category?.name || 'Unknown';
  }

  getImageUrl(imageUrl?: string | null): string {
    if (!imageUrl) return '';
    if (imageUrl.startsWith('http')) return imageUrl;
    return `${environment.apiUrl}/${imageUrl}`;
  }
}
