import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { CategoryManagementService, CategoryDetails } from '../../services/category-management.service';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

@Component({
  selector: 'app-category-form',
  templateUrl: './category-form.component.html',
  styleUrls: ['./category-form.component.css']
})
export class CategoryFormComponent implements OnInit {
  categoryForm: FormGroup;
  loading = false;
  isEditMode = false;
  categoryId: string | null = null;
  autoGenerateSlug = true;
  slugCheckLoading = false;
  slugAvailable = true;

  constructor(
    private fb: FormBuilder,
    private categoryService: CategoryManagementService,
    private router: Router,
    private route: ActivatedRoute,
    private message: NzMessageService
  ) {
    this.categoryForm = this.createForm();
  }

  ngOnInit(): void {
    this.categoryId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.categoryId;

    if (this.isEditMode && this.categoryId) {
      this.loadCategory();
    }

    this.setupSlugGeneration();
    this.setupSlugValidation();
  }

  private createForm(): FormGroup {
    return this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      slug: ['', [Validators.required, Validators.maxLength(100), Validators.pattern(/^[a-z0-9-]+$/)]],
      description: ['', [Validators.maxLength(500)]]
    });
  }

  private setupSlugGeneration(): void {
    this.categoryForm.get('name')?.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(name => {
      if (this.autoGenerateSlug && name) {
        const generatedSlug = this.categoryService.generateSlug(name);
        this.categoryForm.patchValue({ slug: generatedSlug }, { emitEvent: false });
        this.checkSlugAvailability(generatedSlug);
      }
    });
  }

  private setupSlugValidation(): void {
    this.categoryForm.get('slug')?.valueChanges.pipe(
      debounceTime(500),
      distinctUntilChanged()
    ).subscribe(slug => {
      if (slug && slug.length > 0) {
        this.checkSlugAvailability(slug);
      }
    });
  }

  private checkSlugAvailability(slug: string): void {
    if (!slug || slug.length === 0) return;

    this.slugCheckLoading = true;
    this.categoryService.checkSlugAvailability(slug, this.categoryId || undefined).subscribe({
      next: (response) => {
        this.slugAvailable = response.data;
        this.slugCheckLoading = false;

        const slugControl = this.categoryForm.get('slug');
        if (!this.slugAvailable && slugControl) {
          slugControl.setErrors({ unavailable: true });
        }
      },
      error: () => {
        this.slugCheckLoading = false;
      }
    });
  }

  onSlugManualEdit(): void {
    this.autoGenerateSlug = false;
  }

  regenerateSlug(): void {
    const name = this.categoryForm.get('name')?.value;
    if (name) {
      const generatedSlug = this.categoryService.generateSlug(name);
      this.categoryForm.patchValue({ slug: generatedSlug });
      this.autoGenerateSlug = true;
      this.checkSlugAvailability(generatedSlug);
    }
  }

  private loadCategory(): void {
    if (!this.categoryId) return;

    this.loading = true;
    this.categoryService.getCategoryById(this.categoryId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.populateForm(response.data);
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

  private populateForm(category: CategoryDetails): void {
    this.autoGenerateSlug = false; // Disable auto-generation when editing
    this.categoryForm.patchValue({
      name: category.name,
      slug: category.slug,
      description: category.description || ''
    });
  }

  onSubmit(): void {
    if (!this.categoryForm.valid) {
      this.markFormGroupTouched();
      return;
    }

    const formData = this.categoryForm.value;
    this.loading = true;

    const operation = this.isEditMode && this.categoryId
      ? this.categoryService.updateCategory(this.categoryId, formData)
      : this.categoryService.createCategory(formData);

    operation.subscribe({
      next: (response) => {
        if (response.success) {
          const action = this.isEditMode ? 'updated' : 'created';
          this.message.success(`Category ${action} successfully`);
          this.router.navigate(['/products/categories']);
        } else {
          this.message.error(response.message || `Failed to ${this.isEditMode ? 'update' : 'create'} category`);
        }
        this.loading = false;
      },
      error: (error) => {
        console.error('Error saving category:', error);
        this.message.error(`Failed to ${this.isEditMode ? 'update' : 'create'} category`);
        this.loading = false;
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/products/categories']);
  }

  private markFormGroupTouched(): void {
    Object.keys(this.categoryForm.controls).forEach(key => {
      const control = this.categoryForm.get(key);
      if (control) {
        control.markAsTouched();
        control.updateValueAndValidity();
      }
    });
  }

  getFieldError(fieldName: string): string {
    const field = this.categoryForm.get(fieldName);
    if (field?.errors && field.touched) {
      if (field.errors['required']) return `${fieldName} is required`;
      if (field.errors['maxlength']) return `${fieldName} is too long`;
      if (field.errors['pattern']) return 'Slug can only contain lowercase letters, numbers, and hyphens';
      if (field.errors['unavailable']) return 'This slug is already taken';
    }
    return '';
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.categoryForm.get(fieldName);
    return !!(field?.invalid && field.touched);
  }
}
