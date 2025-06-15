import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalService } from 'ng-zorro-antd/modal';
import { ProductManagementService, ProductDetails } from '../../services/product-management.service';

@Component({
  selector: 'app-product-details',
  templateUrl: './product-details.component.html',
  styleUrls: ['./product-details.component.css']
})
export class ProductDetailsComponent implements OnInit {
  product: ProductDetails | null = null;
  loading = false;
  productId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private productService: ProductManagementService,
    private message: NzMessageService,
    private modal: NzModalService
  ) {}

  ngOnInit(): void {
    this.productId = this.route.snapshot.paramMap.get('id');
    if (this.productId) {
      this.loadProduct();
    } else {
      this.router.navigate(['/products']);
    }
  }

  loadProduct(): void {
    if (!this.productId) return;

    this.loading = true;
    this.productService.getProductById(this.productId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.product = response.data;
        } else {
          this.message.error('Failed to load product details');
          this.router.navigate(['/products']);
        }
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading product:', error);
        this.message.error('Failed to load product details');
        this.router.navigate(['/products']);
        this.loading = false;
      }
    });
  }

  editProduct(): void {
    if (this.product) {
      this.router.navigate(['/products', this.product.productId, 'edit']);
    }
  }

  deleteProduct(): void {
    if (!this.product) return;

    this.modal.confirm({
      nzTitle: 'Delete Product',
      nzContent: `Are you sure you want to delete "${this.product.name}"?`,
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
    if (!this.product) return;

    this.productService.deleteProduct(this.product.productId).subscribe({
      next: (response) => {
        if (response.success) {
          this.message.success(`Product "${this.product!.name}" deleted successfully`);
          this.router.navigate(['/products']);
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

  toggleProductStatus(): void {
    if (!this.product) return;

    const newStatus = !this.product.isActive;
    this.productService.toggleProductStatus(this.product.productId, newStatus).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.product = response.data;
          this.message.success(`Product ${newStatus ? 'activated' : 'deactivated'} successfully`);
        } else {
          this.message.error('Failed to update product status');
        }
      },
      error: (error) => {
        console.error('Error updating product status:', error);
        this.message.error('Failed to update product status');
      }
    });
  }

  viewCategory(): void {
    if (this.product) {
      this.router.navigate(['/products/categories', this.product.categoryId]);
    }
  }

  goBack(): void {
    this.router.navigate(['/products']);
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString();
  }

  isJsonString(str: string): boolean {
    try {
      JSON.parse(str);
      return true;
    } catch {
      return false;
    }
  }

  formatSpecifications(specs: string): any {
    if (this.isJsonString(specs)) {
      return JSON.parse(specs);
    }
    return specs;
  }
}
