import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalService } from 'ng-zorro-antd/modal';
import { CategoryManagementService, CategoryDetails } from '../../services/category-management.service';

@Component({
  selector: 'app-category-details',
  templateUrl: './category-details.component.html',
  styleUrls: ['./category-details.component.css']
})
export class CategoryDetailsComponent implements OnInit {
  category: CategoryDetails | null = null;
  loading = false;
  categoryId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private categoryService: CategoryManagementService,
    private message: NzMessageService,
    private modal: NzModalService
  ) {}

  ngOnInit(): void {
    this.categoryId = this.route.snapshot.paramMap.get('id');
    if (this.categoryId) {
      this.loadCategory();
    } else {
      this.router.navigate(['/products/categories']);
    }
  }

  loadCategory(): void {
    if (!this.categoryId) return;

    this.loading = true;
    this.categoryService.getCategoryById(this.categoryId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.category = response.data;
        } else {
          this.message.error('Failed to load category details');
          this.router.navigate(['/products/categories']);
        }
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading category:', error);
        this.message.error('Failed to load category details');
        this.router.navigate(['/products/categories']);
        this.loading = false;
      }
    });
  }

  editCategory(): void {
    if (this.category) {
      this.router.navigate(['/products/categories', this.category.categoryId, 'edit']);
    }
  }

  deleteCategory(): void {
    if (!this.category) return;

    this.modal.confirm({
      nzTitle: 'Delete Category',
      nzContent: `Are you sure you want to delete the category "${this.category.name}"?`,
      nzOkText: 'Delete',
      nzOkType: 'primary',
      nzOkDanger: true,
      nzCancelText: 'Cancel',
      nzOnOk: () => {
        this.performDelete();
      }
    });
  }

  private performDelete(): void {
    if (!this.category) return;

    this.categoryService.deleteCategory(this.category.categoryId).subscribe({
      next: (response) => {
        if (response.success) {
          this.message.success(`Category "${this.category!.name}" deleted successfully`);
          this.router.navigate(['/products/categories']);
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

  viewProducts(): void {
    if (this.category) {
      this.router.navigate(['/products'], {
        queryParams: { categoryId: this.category.categoryId }
      });
    }
  }

  goBack(): void {
    this.router.navigate(['/products/categories']);
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString();
  }
}
