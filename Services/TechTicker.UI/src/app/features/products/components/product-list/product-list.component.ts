import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalService } from 'ng-zorro-antd/modal';
import { ProductManagementService, ProductListItem, ProductQueryParams } from '../../services/product-management.service';
import { CategoryManagementService, CategoryListItem } from '../../services/category-management.service';

@Component({
  selector: 'app-product-list',
  templateUrl: './product-list.component.html',
  styleUrls: ['./product-list.component.css'],
  standalone: false
})
export class ProductListComponent implements OnInit {
  products: ProductListItem[] = [];
  categories: CategoryListItem[] = [];
  manufacturers: string[] = [];
  loading = false;
  categoriesLoading = false;

  // Search and filters
  searchValue = '';
  selectedCategoryId = '';
  selectedManufacturer = '';
  activeFilter: boolean | undefined = undefined;

  // Pagination
  pageIndex = 1;
  pageSize = 10;
  total = 0;

  // Selection
  checked = false;
  indeterminate = false;
  setOfCheckedId = new Set<string>();

  // Table columns
  listOfColumns = [
    {
      name: 'Product',
      sortOrder: null,
      sortFn: (a: ProductListItem, b: ProductListItem) => a.name.localeCompare(b.name),
      sortDirections: ['ascend', 'descend', null]
    },
    {
      name: 'SKU',
      sortOrder: null,
      sortFn: (a: ProductListItem, b: ProductListItem) => (a.sku || '').localeCompare(b.sku || ''),
      sortDirections: ['ascend', 'descend', null]
    },
    {
      name: 'Category',
      sortOrder: null,
      sortFn: (a: ProductListItem, b: ProductListItem) => a.categoryName.localeCompare(b.categoryName),
      sortDirections: ['ascend', 'descend', null]
    },
    {
      name: 'Manufacturer',
      sortOrder: null,
      sortFn: (a: ProductListItem, b: ProductListItem) => (a.manufacturer || '').localeCompare(b.manufacturer || ''),
      sortDirections: ['ascend', 'descend', null]
    },
    {
      name: 'Status',
      sortOrder: null,
      sortFn: (a: ProductListItem, b: ProductListItem) => Number(a.isActive) - Number(b.isActive),
      sortDirections: ['ascend', 'descend', null]
    },
    {
      name: 'Created',
      sortOrder: 'descend',
      sortFn: (a: ProductListItem, b: ProductListItem) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
      sortDirections: ['ascend', 'descend', null]
    }
  ];

  constructor(
    private productService: ProductManagementService,
    private categoryService: CategoryManagementService,
    private router: Router,
    private route: ActivatedRoute,
    private message: NzMessageService,
    private modal: NzModalService
  ) {}

  ngOnInit(): void {
    this.loadCategories();
    this.loadManufacturers();

    // Check for category filter from query params
    this.route.queryParams.subscribe(params => {
      if (params['categoryId']) {
        this.selectedCategoryId = params['categoryId'];
      }
      this.loadProducts();
    });
  }

  loadProducts(): void {
    this.loading = true;

    const params: ProductQueryParams = {
      page: this.pageIndex,
      pageSize: this.pageSize
    };

    if (this.searchValue.trim()) {
      params.search = this.searchValue.trim();
    }

    if (this.selectedCategoryId) {
      params.categoryId = this.selectedCategoryId;
    }

    if (this.selectedManufacturer) {
      params.manufacturer = this.selectedManufacturer;
    }

    if (this.activeFilter !== undefined) {
      params.isActive = this.activeFilter;
    }

    this.productService.getProducts(params).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.products = response.data.items;
          this.total = response.data.totalCount;
          this.updateCheckedSet();
        } else {
          this.message.error(response.message || 'Failed to load products');
        }
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading products:', error);
        this.message.error('Failed to load products');
        this.loading = false;
      }
    });
  }

  loadCategories(): void {
    this.categoriesLoading = true;
    this.categoryService.getAllCategories().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.categories = response.data;
        }
        this.categoriesLoading = false;
      },
      error: (error) => {
        console.error('Error loading categories:', error);
        this.categoriesLoading = false;
      }
    });
  }

  loadManufacturers(): void {
    this.productService.getManufacturers().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.manufacturers = response.data;
        }
      },
      error: (error) => {
        console.error('Error loading manufacturers:', error);
      }
    });
  }

  onSearch(): void {
    this.pageIndex = 1;
    this.loadProducts();
  }

  onFilterChange(): void {
    this.pageIndex = 1;
    this.loadProducts();
  }

  clearFilters(): void {
    this.searchValue = '';
    this.selectedCategoryId = '';
    this.selectedManufacturer = '';
    this.activeFilter = undefined;
    this.pageIndex = 1;
    this.loadProducts();
  }

  onPageIndexChange(pageIndex: number): void {
    this.pageIndex = pageIndex;
    this.loadProducts();
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.pageIndex = 1;
    this.loadProducts();
  }

  // Selection methods
  updateCheckedSet(): void {
    this.setOfCheckedId.clear();
    this.refreshCheckedStatus();
  }

  onItemChecked(id: string, checked: boolean): void {
    if (checked) {
      this.setOfCheckedId.add(id);
    } else {
      this.setOfCheckedId.delete(id);
    }
    this.refreshCheckedStatus();
  }

  onAllChecked(checked: boolean): void {
    this.products.forEach(product => {
      if (checked) {
        this.setOfCheckedId.add(product.productId);
      } else {
        this.setOfCheckedId.delete(product.productId);
      }
    });
    this.refreshCheckedStatus();
  }

  refreshCheckedStatus(): void {
    const listOfEnabledData = this.products;
    this.checked = listOfEnabledData.every(product => this.setOfCheckedId.has(product.productId));
    this.indeterminate = listOfEnabledData.some(product => this.setOfCheckedId.has(product.productId)) && !this.checked;
  }

  // Actions
  viewProduct(product: ProductListItem): void {
    this.router.navigate(['/products', product.productId]);
  }

  editProduct(product: ProductListItem): void {
    this.router.navigate(['/products', product.productId, 'edit']);
  }

  createProduct(): void {
    this.router.navigate(['/products/create']);
  }

  deleteProduct(product: ProductListItem): void {
    this.modal.confirm({
      nzTitle: 'Delete Product',
      nzContent: `Are you sure you want to delete "${product.name}"?`,
      nzOkText: 'Delete',
      nzOkType: 'primary',
      nzOkDanger: true,
      nzCancelText: 'Cancel',
      nzOnOk: () => {
        this.performDelete(product);
      }
    });
  }

  bulkDelete(): void {
    if (this.setOfCheckedId.size === 0) {
      this.message.warning('Please select products to delete');
      return;
    }

    this.modal.confirm({
      nzTitle: 'Bulk Delete Products',
      nzContent: `Are you sure you want to delete ${this.setOfCheckedId.size} selected products?`,
      nzOkText: 'Delete All',
      nzOkType: 'primary',
      nzOkDanger: true,
      nzCancelText: 'Cancel',
      nzOnOk: () => {
        this.performBulkDelete();
      }
    });
  }

  exportProducts(): void {
    this.productService.exportProducts({ format: 'csv' }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `products-export-${new Date().toISOString().split('T')[0]}.csv`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.message.success('Products exported successfully');
      },
      error: (error) => {
        console.error('Error exporting products:', error);
        this.message.error('Failed to export products');
      }
    });
  }

  private performDelete(product: ProductListItem): void {
    this.productService.deleteProduct(product.productId).subscribe({
      next: (response) => {
        if (response.success) {
          this.message.success(`Product "${product.name}" deleted successfully`);
          this.loadProducts();
        } else {
          this.message.error(response.message || 'Failed to delete product');
        }
      },
      error: (error) => {
        console.error('Error deleting product:', error);
        this.message.error('Failed to delete product');
      }
    });
  }

  private performBulkDelete(): void {
    const productIds = Array.from(this.setOfCheckedId);
    this.productService.bulkDeleteProducts(productIds).subscribe({
      next: (response) => {
        if (response.success) {
          this.message.success(`${productIds.length} products deleted successfully`);
          this.setOfCheckedId.clear();
          this.loadProducts();
        } else {
          this.message.error(response.message || 'Failed to delete products');
        }
      },
      error: (error) => {
        console.error('Error bulk deleting products:', error);
        this.message.error('Failed to delete products');
      }
    });
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }

  getEmptyStateContent(): string {
    if (this.hasActiveFilters()) {
      return 'No products found matching your search criteria';
    }
    return 'No products available';
  }

  hasActiveFilters(): boolean {
    return !!(this.searchValue.trim() ||
              this.selectedCategoryId ||
              this.selectedManufacturer ||
              this.activeFilter !== undefined);
  }
}
