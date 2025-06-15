import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ProductDto, CreateProductDto, UpdateProductDto, CategoryDto } from '../../../../shared/api/api-client';
import { ProductsService } from '../../services/products.service';
import { CategoriesService } from '../../../categories/services/categories.service';

@Component({
  selector: 'app-product-form',
  templateUrl: './product-form.component.html',
  styleUrls: ['./product-form.component.scss'],
  standalone: false
})
export class ProductFormComponent implements OnInit {
  productForm: FormGroup;
  isLoading = false;
  isEditMode = false;
  productId: string | null = null;
  categories: CategoryDto[] = [];

  constructor(
    private formBuilder: FormBuilder,
    private productsService: ProductsService,
    private categoriesService: CategoriesService,
    private router: Router,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar
  ) {
    this.productForm = this.formBuilder.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.maxLength(1000)]],
      manufacturer: ['', [Validators.maxLength(100)]],
      modelNumber: ['', [Validators.maxLength(100)]],
      categoryId: ['', [Validators.required]],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    this.productId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.productId;

    this.loadCategories();

    if (this.isEditMode && this.productId) {
      this.loadProduct(this.productId);
    }
  }

  loadCategories(): void {
    this.categoriesService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories;
      },
      error: (error) => {
        console.error('Error loading categories:', error);
        this.snackBar.open('Failed to load categories', 'Close', { duration: 5000 });
      }
    });
  }

  loadProduct(id: string): void {
    this.isLoading = true;
    this.productsService.getProduct(id).subscribe({
      next: (product) => {
        this.productForm.patchValue({
          name: product.name,
          description: product.description,
          manufacturer: product.manufacturer,
          modelNumber: product.modelNumber,
          categoryId: product.categoryId,
          isActive: product.isActive
        });
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading product:', error);
        this.snackBar.open('Failed to load product', 'Close', { duration: 5000 });
        this.router.navigate(['/products']);
      }
    });
  }

  onSubmit(): void {
    if (this.productForm.valid && !this.isLoading) {
      this.isLoading = true;

      const formValue = this.productForm.value;

      if (this.isEditMode && this.productId) {
        const updateDto = new UpdateProductDto({
          name: formValue.name,
          description: formValue.description,
          manufacturer: formValue.manufacturer,
          modelNumber: formValue.modelNumber,
          categoryId: formValue.categoryId,
          isActive: formValue.isActive
        });

        this.productsService.updateProduct(this.productId, updateDto).subscribe({
          next: () => {
            this.snackBar.open('Product updated successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/products']);
          },
          error: (error) => {
            console.error('Error updating product:', error);
            this.snackBar.open('Failed to update product', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      } else {
        const createDto = new CreateProductDto({
          name: formValue.name,
          description: formValue.description,
          manufacturer: formValue.manufacturer,
          modelNumber: formValue.modelNumber,
          categoryId: formValue.categoryId
        });

        this.productsService.createProduct(createDto).subscribe({
          next: () => {
            this.snackBar.open('Product created successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/products']);
          },
          error: (error) => {
            console.error('Error creating product:', error);
            this.snackBar.open('Failed to create product', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      }
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel(): void {
    this.router.navigate(['/products']);
  }

  private markFormGroupTouched(): void {
    Object.keys(this.productForm.controls).forEach(key => {
      const control = this.productForm.get(key);
      if (control) {
        control.markAsTouched();
      }
    });
  }

  getFieldErrorMessage(fieldName: string): string {
    const control = this.productForm.get(fieldName);
    if (control?.hasError('required')) {
      return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} is required`;
    }
    if (control?.hasError('maxlength')) {
      const maxLength = control.errors?.['maxlength']?.requiredLength;
      return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} must be less than ${maxLength} characters`;
    }
    return '';
  }
}
