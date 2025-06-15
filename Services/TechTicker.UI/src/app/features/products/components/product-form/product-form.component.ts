import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { ProductManagementService, ProductDetails } from '../../services/product-management.service';
import { CategoryManagementService, CategoryListItem } from '../../services/category-management.service';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

@Component({
  selector: 'app-product-form',
  templateUrl: './product-form.component.html',
  styleUrls: ['./product-form.component.css']
})
export class ProductFormComponent implements OnInit {
  productForm: FormGroup;
  loading = false;
  categoriesLoading = false;
  isEditMode = false;
  productId: string | null = null;
  categories: CategoryListItem[] = [];
  skuCheckLoading = false;
  skuAvailable = true;

  constructor(
    private fb: FormBuilder,
    private productService: ProductManagementService,
    private categoryService: CategoryManagementService,
    private router: Router,
    private route: ActivatedRoute,
    private message: NzMessageService
  ) {
    this.productForm = this.createForm();
  }

  ngOnInit(): void {
    this.productId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.productId;

    this.loadCategories();

    if (this.isEditMode && this.productId) {
      this.loadProduct();
    }

    this.setupSkuValidation();
  }

  private createForm(): FormGroup {
    return this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(255)]],
      sku: ['', [Validators.maxLength(100)]],
      manufacturer: ['', [Validators.maxLength(100)]],
      modelNumber: ['', [Validators.maxLength(100)]],
      categoryId: ['', [Validators.required]],
      description: [''],
      specifications: [''],
      isActive: [true]
    });
  }

  private setupSkuValidation(): void {
    this.productForm.get('sku')?.valueChanges.pipe(
      debounceTime(500),
      distinctUntilChanged()
    ).subscribe(sku => {
      if (sku && sku.length > 0) {
        this.checkSkuAvailability(sku);
      } else {
        this.skuAvailable = true;
        this.skuCheckLoading = false;
      }
    });
  }

  private checkSkuAvailability(sku: string): void {
    if (!sku || sku.length === 0) return;

    this.skuCheckLoading = true;
    this.productService.checkSkuAvailability(sku, this.productId || undefined).subscribe({
      next: (response) => {
        this.skuAvailable = response.data;
        this.skuCheckLoading = false;

        const skuControl = this.productForm.get('sku');
        if (!this.skuAvailable && skuControl) {
          skuControl.setErrors({ unavailable: true });
        }
      },
      error: () => {
        this.skuCheckLoading = false;
      }
    });
  }

  private loadCategories(): void {
    this.categoriesLoading = true;
    this.categoryService.getAllCategories().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.categories = response.data;
        } else {
          this.message.error('Failed to load categories');
        }
        this.categoriesLoading = false;
      },
      error: (error) => {
        console.error('Error loading categories:', error);
        this.message.error('Failed to load categories');
        this.categoriesLoading = false;
      }
    });
  }

  private loadProduct(): void {
    if (!this.productId) return;

    this.loading = true;
    this.productService.getProductById(this.productId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.populateForm(response.data);
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

  private populateForm(product: ProductDetails): void {
    this.productForm.patchValue({
      name: product.name,
      sku: product.sku || '',
      manufacturer: product.manufacturer || '',
      modelNumber: product.modelNumber || '',
      categoryId: product.categoryId,
      description: product.description || '',
      specifications: product.specifications || '',
      isActive: product.isActive
    });
  }

  onSubmit(): void {
    if (!this.productForm.valid) {
      this.markFormGroupTouched();
      return;
    }

    if (!this.skuAvailable) {
      this.message.error('Please fix the SKU availability issue before saving');
      return;
    }

    const formData = this.productForm.value;
    this.loading = true;

    const operation = this.isEditMode && this.productId
      ? this.productService.updateProduct(this.productId, formData)
      : this.productService.createProduct(formData);

    operation.subscribe({
      next: (response) => {
        if (response.success) {
          const action = this.isEditMode ? 'updated' : 'created';
          this.message.success(`Product ${action} successfully`);
          this.router.navigate(['/products']);
        } else {
          this.message.error(response.message || `Failed to ${this.isEditMode ? 'update' : 'create'} product`);
        }
        this.loading = false;
      },
      error: (error) => {
        console.error('Error saving product:', error);
        this.message.error(`Failed to ${this.isEditMode ? 'update' : 'create'} product`);
        this.loading = false;
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/products']);
  }

  private markFormGroupTouched(): void {
    Object.keys(this.productForm.controls).forEach(key => {
      const control = this.productForm.get(key);
      if (control) {
        control.markAsTouched();
        control.updateValueAndValidity();
      }
    });
  }

  getFieldError(fieldName: string): string {
    const field = this.productForm.get(fieldName);
    if (field?.errors && field.touched) {
      if (field.errors['required']) return `${this.getFieldLabel(fieldName)} is required`;
      if (field.errors['maxlength']) return `${this.getFieldLabel(fieldName)} is too long`;
      if (field.errors['unavailable']) return 'This SKU is already taken';
    }
    return '';
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      name: 'Product Name',
      sku: 'SKU',
      manufacturer: 'Manufacturer',
      modelNumber: 'Model Number',
      categoryId: 'Category',
      description: 'Description',
      specifications: 'Specifications'
    };
    return labels[fieldName] || fieldName;
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.productForm.get(fieldName);
    return !!(field?.invalid && field.touched);
  }

  navigateToCategories(): void {
    this.router.navigate(['/products/categories']);
  }
}
