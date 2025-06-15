import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalService } from 'ng-zorro-antd/modal';
import { CategoryManagementService, CategoryListItem, CategoryQueryParams } from '../../services/category-management.service';

@Component({
  selector: 'app-category-list',
  templateUrl: './category-list.component.html',
  styleUrls: ['./category-list.component.css']
})
export class CategoryListComponent implements OnInit {
  categories: CategoryListItem[] = [];
  loading = false;
  searchValue = '';

  // Pagination
  pageIndex = 1;
  pageSize = 10;
  total = 0;

  // Table columns
  listOfColumns = [
    {
      name: 'Name',
      sortOrder: null,
      sortFn: (a: CategoryListItem, b: CategoryListItem) => a.name.localeCompare(b.name),
      sortDirections: ['ascend', 'descend', null]
    },
    {
      name: 'Slug',
      sortOrder: null,
      sortFn: (a: CategoryListItem, b: CategoryListItem) => a.slug.localeCompare(b.slug),
      sortDirections: ['ascend', 'descend', null]
    },
    {
      name: 'Products',
      sortOrder: null,
      sortFn: (a: CategoryListItem, b: CategoryListItem) => (a.productCount || 0) - (b.productCount || 0),
      sortDirections: ['ascend', 'descend', null]
    },
    {
      name: 'Created',
      sortOrder: 'descend',
      sortFn: (a: CategoryListItem, b: CategoryListItem) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
      sortDirections: ['ascend', 'descend', null]
    }
  ];

  constructor(
    private categoryService: CategoryManagementService,
    private router: Router,
    private message: NzMessageService,
    private modal: NzModalService
  ) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading = true;

    const params: CategoryQueryParams = {
      page: this.pageIndex,
      pageSize: this.pageSize
    };

    if (this.searchValue.trim()) {
      params.search = this.searchValue.trim();
    }

    this.categoryService.getCategories(params).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.categories = response.data.items;
          this.total = response.data.totalCount;
        } else {
          this.message.error(response.message || 'Failed to load categories');
        }
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading categories:', error);
        this.message.error('Failed to load categories');
        this.loading = false;
      }
    });
  }

  onSearch(): void {
    this.pageIndex = 1;
    this.loadCategories();
  }

  onSearchClear(): void {
    this.searchValue = '';
    this.pageIndex = 1;
    this.loadCategories();
  }

  onPageIndexChange(pageIndex: number): void {
    this.pageIndex = pageIndex;
    this.loadCategories();
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.pageIndex = 1;
    this.loadCategories();
  }

  viewCategory(category: CategoryListItem): void {
    this.router.navigate(['/products/categories', category.categoryId]);
  }

  editCategory(category: CategoryListItem): void {
    this.router.navigate(['/products/categories', category.categoryId, 'edit']);
  }

  createCategory(): void {
    this.router.navigate(['/products/categories/create']);
  }

  deleteCategory(category: CategoryListItem): void {
    this.modal.confirm({
      nzTitle: 'Delete Category',
      nzContent: `Are you sure you want to delete the category "${category.name}"?`,
      nzOkText: 'Delete',
      nzOkType: 'primary',
      nzOkDanger: true,
      nzCancelText: 'Cancel',
      nzOnOk: () => {
        this.performDelete(category);
      }
    });
  }

  private performDelete(category: CategoryListItem): void {
    this.categoryService.deleteCategory(category.categoryId).subscribe({
      next: (response) => {
        if (response.success) {
          this.message.success(`Category "${category.name}" deleted successfully`);
          this.loadCategories();
        } else {
          this.message.error(response.message || 'Failed to delete category');
        }
      },
      error: (error) => {
        console.error('Error deleting category:', error);
        if (error.status === 409) {
          this.message.error('Cannot delete category with existing products');
        } else {
          this.message.error('Failed to delete category');
        }
      }
    });
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }
}
