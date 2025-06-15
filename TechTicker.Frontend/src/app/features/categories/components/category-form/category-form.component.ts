import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CategoryDto, CreateCategoryDto, UpdateCategoryDto } from '../../../../shared/api/api-client';
import { CategoriesService } from '../../services/categories.service';

@Component({
  selector: 'app-category-form',
  templateUrl: './category-form.component.html',
  styleUrls: ['./category-form.component.scss'],
  standalone: false
})
export class CategoryFormComponent implements OnInit {
  categoryForm: FormGroup;
  isLoading = false;
  isEditMode = false;
  categoryId: string | null = null;

  constructor(
    private formBuilder: FormBuilder,
    private categoriesService: CategoriesService,
    private router: Router,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar
  ) {
    this.categoryForm = this.formBuilder.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      slug: ['', [Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(500)]]
    });
  }

  ngOnInit(): void {
    this.categoryId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.categoryId;

    if (this.isEditMode && this.categoryId) {
      this.loadCategory(this.categoryId);
    }

    // Auto-generate slug from name
    this.categoryForm.get('name')?.valueChanges.subscribe(name => {
      if (name && !this.categoryForm.get('slug')?.dirty) {
        const slug = this.generateSlug(name);
        this.categoryForm.patchValue({ slug }, { emitEvent: false });
      }
    });
  }

  loadCategory(id: string): void {
    this.isLoading = true;
    this.categoriesService.getCategory(id).subscribe({
      next: (category) => {
        this.categoryForm.patchValue({
          name: category.name,
          slug: category.slug,
          description: category.description
        });
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading category:', error);
        this.snackBar.open('Failed to load category', 'Close', { duration: 5000 });
        this.router.navigate(['/categories']);
      }
    });
  }

  generateSlug(name: string): string {
    return name
      .toLowerCase()
      .replace(/[^a-z0-9\s-]/g, '')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-')
      .trim();
  }

  onSubmit(): void {
    if (this.categoryForm.valid && !this.isLoading) {
      this.isLoading = true;

      const formValue = this.categoryForm.value;
      
      if (this.isEditMode && this.categoryId) {
        const updateDto = new UpdateCategoryDto({
          name: formValue.name,
          slug: formValue.slug,
          description: formValue.description
        });

        this.categoriesService.updateCategory(this.categoryId, updateDto).subscribe({
          next: () => {
            this.snackBar.open('Category updated successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/categories']);
          },
          error: (error) => {
            console.error('Error updating category:', error);
            this.snackBar.open('Failed to update category', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      } else {
        const createDto = new CreateCategoryDto({
          name: formValue.name,
          slug: formValue.slug,
          description: formValue.description
        });

        this.categoriesService.createCategory(createDto).subscribe({
          next: () => {
            this.snackBar.open('Category created successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/categories']);
          },
          error: (error) => {
            console.error('Error creating category:', error);
            this.snackBar.open('Failed to create category', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      }
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel(): void {
    this.router.navigate(['/categories']);
  }

  private markFormGroupTouched(): void {
    Object.keys(this.categoryForm.controls).forEach(key => {
      const control = this.categoryForm.get(key);
      if (control) {
        control.markAsTouched();
      }
    });
  }

  getFieldErrorMessage(fieldName: string): string {
    const control = this.categoryForm.get(fieldName);
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
