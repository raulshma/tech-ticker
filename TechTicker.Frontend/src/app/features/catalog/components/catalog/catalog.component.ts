import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormControl } from '@angular/forms';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSnackBar } from '@angular/material/snack-bar';
import { debounceTime, distinctUntilChanged, startWith, combineLatest } from 'rxjs';
import { CategoryDto, ProductWithCurrentPricesDto } from '../../../../shared/api/api-client';
import { CatalogService, CatalogFilter, PagedResult } from '../../services/catalog.service';
import { CategoriesService } from '../../../categories/services/categories.service';

@Component({
  selector: 'app-catalog',
  templateUrl: './catalog.component.html',
  styleUrls: ['./catalog.component.scss'],
  standalone: false
})
export class CatalogComponent implements OnInit {
  products: ProductWithCurrentPricesDto[] = [];
  isLoading = false;

  // Pagination
  totalCount = 0;
  pageSize = 12;
  currentPage = 0;
  pageSizeOptions = [12, 24, 48];

  // Filters
  searchControl = new FormControl('');
  categoryControl = new FormControl('');
  categories: CategoryDto[] = [];
  selectedCategorySlug: string | null = null;

  // View options
  viewMode: 'grid' | 'list' = 'grid';

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  constructor(
    private catalogService: CatalogService,
    private categoriesService: CategoriesService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadCategories();
    this.setupFilters();
    this.handleRouteParams();
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

  handleRouteParams(): void {
    this.route.params.subscribe(params => {
      if (params['slug']) {
        this.selectedCategorySlug = params['slug'];
        const category = this.categories.find(c => c.slug === params['slug']);
        if (category && category.categoryId) {
          this.categoryControl.setValue(category.categoryId);
        }
      } else {
        this.selectedCategorySlug = null;
        this.categoryControl.setValue('');
      }
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

    const filter: CatalogFilter = {
      search: this.searchControl.value || undefined,
      categoryId: this.categoryControl.value || undefined,
      page: this.currentPage + 1,
      pageSize: this.pageSize
    };

    this.catalogService.getProductsCatalog(filter).subscribe({
      next: (result: PagedResult<ProductWithCurrentPricesDto>) => {
        this.products = result.items;
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

  onCategorySelect(categoryId: string): void {
    if (categoryId) {
      const category = this.categories.find(c => c.categoryId === categoryId);
      if (category) {
        this.router.navigate(['/catalog/category', category.slug]);
      }
    } else {
      this.router.navigate(['/catalog']);
    }
  }

  onProductClick(product: ProductWithCurrentPricesDto): void {
    this.router.navigate(['/catalog/product', product.productId]);
  }

  toggleViewMode(): void {
    this.viewMode = this.viewMode === 'grid' ? 'list' : 'grid';
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.categoryControl.setValue('');
    this.router.navigate(['/catalog']);
  }

  getSelectedCategoryName(): string {
    if (!this.selectedCategorySlug) return '';
    const category = this.categories.find(c => c.slug === this.selectedCategorySlug);
    return category?.name || '';
  }

  getEndIndex(): number {
    return Math.min((this.currentPage + 1) * this.pageSize, this.totalCount);
  }
}
