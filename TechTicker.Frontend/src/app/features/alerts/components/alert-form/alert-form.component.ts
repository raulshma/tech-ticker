import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { Observable, startWith, map } from 'rxjs';

import { AlertsService } from '../../services/alerts.service';
import { ProductsService } from '../../../products/services/products.service';
import {
  AlertRuleDto,
  CreateAlertRuleDto,
  UpdateAlertRuleDto,
  ProductDto
} from '../../../../shared/api/api-client';

@Component({
  selector: 'app-alert-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule,
    MatSnackBarModule,
    MatAutocompleteModule
  ],
  template: `
    <mat-card class="alert-form-card">
      <mat-card-header>
        <mat-card-title>
          {{ isEditMode ? 'Edit Alert Rule' : 'Create Alert Rule' }}
        </mat-card-title>
        <mat-card-subtitle>
          Set up price and availability notifications
        </mat-card-subtitle>
      </mat-card-header>

      <mat-card-content>
        <form [formGroup]="alertForm" (ngSubmit)="onSubmit()">
          <!-- Product Selection -->
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Product</mat-label>
            <input
              type="text"
              matInput
              formControlName="productSearch"
              [matAutocomplete]="productAuto"
              placeholder="Search for a product...">
            <mat-autocomplete #productAuto="matAutocomplete" [displayWith]="displayProduct">
              <mat-option
                *ngFor="let product of filteredProducts | async"
                [value]="product"
                (onSelectionChange)="onProductSelected(product)">
                <div class="product-option">
                  <div class="product-name">{{ product.name }}</div>
                  <div class="product-details">{{ product.manufacturer }} - {{ product.category?.name }}</div>
                </div>
              </mat-option>
            </mat-autocomplete>
            <mat-error *ngIf="alertForm.get('productId')?.hasError('required')">
              Product is required
            </mat-error>
          </mat-form-field>

          <!-- Seller Name -->
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Seller (Optional)</mat-label>
            <input matInput formControlName="sellerName" placeholder="Specific seller to monitor">
            <mat-hint>Leave empty to monitor all sellers</mat-hint>
          </mat-form-field>

          <!-- Alert Type -->
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Alert Type</mat-label>
            <mat-select formControlName="alertType">
              <mat-option value="RECURRING">Recurring</mat-option>
              <mat-option value="ONE_SHOT">One-time</mat-option>
            </mat-select>
            <mat-hint>Recurring alerts continue until disabled, one-time alerts trigger once</mat-hint>
          </mat-form-field>

          <!-- Condition Type -->
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Condition</mat-label>
            <mat-select formControlName="conditionType" (selectionChange)="onConditionTypeChange()">
              <mat-option value="PRICE_BELOW">Price drops below</mat-option>
              <mat-option value="PERCENT_DROP_FROM_LAST">Percentage drop from last price</mat-option>
              <mat-option value="BACK_IN_STOCK">Back in stock</mat-option>
            </mat-select>
          </mat-form-field>

          <!-- Condition Value (for price-based conditions) -->
          <mat-form-field
            appearance="outline"
            class="full-width"
            *ngIf="showConditionValue()">
            <mat-label>{{ getConditionValueLabel() }}</mat-label>
            <input
              matInput
              type="number"
              formControlName="conditionValue"
              [placeholder]="getConditionValuePlaceholder()">
            <span matSuffix *ngIf="alertForm.get('conditionType')?.value === 'PRICE_BELOW'">$</span>
            <span matSuffix *ngIf="alertForm.get('conditionType')?.value === 'PERCENT_DROP_FROM_LAST'">%</span>
            <mat-error *ngIf="alertForm.get('conditionValue')?.hasError('required')">
              {{ getConditionValueLabel() }} is required
            </mat-error>
            <mat-error *ngIf="alertForm.get('conditionValue')?.hasError('min')">
              Value must be greater than 0
            </mat-error>
          </mat-form-field>

          <!-- Notification Frequency -->
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Notification Frequency (minutes)</mat-label>
            <mat-select formControlName="notificationFrequencyMinutes">
              <mat-option [value]="0">Immediate</mat-option>
              <mat-option [value]="15">Every 15 minutes</mat-option>
              <mat-option [value]="30">Every 30 minutes</mat-option>
              <mat-option [value]="60">Every hour</mat-option>
              <mat-option [value]="240">Every 4 hours</mat-option>
              <mat-option [value]="1440">Daily</mat-option>
            </mat-select>
            <mat-hint>Minimum time between notifications for the same alert</mat-hint>
          </mat-form-field>

          <!-- Description -->
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Description (Optional)</mat-label>
            <textarea
              matInput
              formControlName="ruleDescription"
              rows="3"
              placeholder="Optional description for this alert rule">
            </textarea>
          </mat-form-field>

          <!-- Active Toggle -->
          <div class="toggle-field">
            <mat-slide-toggle formControlName="isActive">
              Active
            </mat-slide-toggle>
            <span class="toggle-hint">Alert will only trigger when active</span>
          </div>
        </form>
      </mat-card-content>

      <mat-card-actions align="end">
        <button mat-button type="button" (click)="onCancel()">
          Cancel
        </button>
        <button
          mat-raised-button
          color="primary"
          (click)="onSubmit()"
          [disabled]="alertForm.invalid || isSubmitting">
          <mat-icon *ngIf="isSubmitting">hourglass_empty</mat-icon>
          {{ isSubmitting ? 'Saving...' : (isEditMode ? 'Update Alert' : 'Create Alert') }}
        </button>
      </mat-card-actions>
    </mat-card>
  `,
  styles: [`
    .alert-form-card {
      max-width: 600px;
      margin: 20px auto;
    }

    .full-width {
      width: 100%;
      margin-bottom: 16px;
    }

    .product-option {
      padding: 8px 0;
    }

    .product-name {
      font-weight: 500;
      margin-bottom: 4px;
    }

    .product-details {
      font-size: 12px;
      color: #666;
    }

    .toggle-field {
      display: flex;
      align-items: center;
      margin: 16px 0;
    }

    .toggle-hint {
      margin-left: 16px;
      font-size: 12px;
      color: #666;
    }

    mat-card-actions {
      padding: 16px 24px;
    }
  `]
})
export class AlertFormComponent implements OnInit {
  @Input() alertRule?: AlertRuleDto;
  @Input() productId?: string;
  @Output() alertSaved = new EventEmitter<AlertRuleDto>();
  @Output() cancelled = new EventEmitter<void>();

  alertForm!: FormGroup;
  isEditMode = false;
  isSubmitting = false;
  products: ProductDto[] = [];
  filteredProducts!: Observable<ProductDto[]>;

  constructor(
    private fb: FormBuilder,
    private alertsService: AlertsService,
    private productsService: ProductsService,
    private snackBar: MatSnackBar
  ) {
    this.initializeForm();
  }

  ngOnInit(): void {
    this.isEditMode = !!this.alertRule;
    this.loadProducts();
    this.setupProductAutocomplete();

    if (this.alertRule) {
      this.populateForm();
    } else if (this.productId) {
      this.alertForm.patchValue({ productId: this.productId });
    }
  }

  private initializeForm(): void {
    this.alertForm = this.fb.group({
      productId: ['', Validators.required],
      productSearch: [''],
      sellerName: [''],
      alertType: ['RECURRING', Validators.required],
      conditionType: ['PRICE_BELOW', Validators.required],
      conditionValue: [null],
      notificationFrequencyMinutes: [60, Validators.required],
      ruleDescription: [''],
      isActive: [true]
    });

    // Set up conditional validation for conditionValue
    this.alertForm.get('conditionType')?.valueChanges.subscribe(() => {
      this.updateConditionValueValidation();
    });
  }

  private loadProducts(): void {
    this.productsService.getAllProducts().subscribe({
      next: (products) => {
        this.products = products;
      },
      error: (error) => {
        console.error('Error loading products:', error);
        this.snackBar.open('Failed to load products', 'Close', { duration: 3000 });
      }
    });
  }

  private setupProductAutocomplete(): void {
    this.filteredProducts = this.alertForm.get('productSearch')!.valueChanges.pipe(
      startWith(''),
      map(value => {
        const searchValue = typeof value === 'string' ? value : value?.name || '';
        return this.filterProducts(searchValue);
      })
    );
  }

  private filterProducts(value: string): ProductDto[] {
    const filterValue = value.toLowerCase();
    return this.products.filter(product =>
      product.name?.toLowerCase().includes(filterValue) ||
      product.manufacturer?.toLowerCase().includes(filterValue)
    );
  }

  displayProduct(product: ProductDto): string {
    return product?.name || '';
  }

  onProductSelected(product: ProductDto): void {
    this.alertForm.patchValue({
      productId: product.productId,
      productSearch: product
    });
  }

  onConditionTypeChange(): void {
    this.updateConditionValueValidation();
  }

  private updateConditionValueValidation(): void {
    const conditionType = this.alertForm.get('conditionType')?.value;
    const conditionValueControl = this.alertForm.get('conditionValue');

    if (conditionType === 'BACK_IN_STOCK') {
      conditionValueControl?.clearValidators();
      conditionValueControl?.setValue(null);
    } else {
      conditionValueControl?.setValidators([Validators.required, Validators.min(0.01)]);
    }
    conditionValueControl?.updateValueAndValidity();
  }

  showConditionValue(): boolean {
    const conditionType = this.alertForm.get('conditionType')?.value;
    return conditionType !== 'BACK_IN_STOCK';
  }

  getConditionValueLabel(): string {
    const conditionType = this.alertForm.get('conditionType')?.value;
    switch (conditionType) {
      case 'PRICE_BELOW':
        return 'Target Price';
      case 'PERCENT_DROP_FROM_LAST':
        return 'Percentage Drop';
      default:
        return 'Value';
    }
  }

  getConditionValuePlaceholder(): string {
    const conditionType = this.alertForm.get('conditionType')?.value;
    switch (conditionType) {
      case 'PRICE_BELOW':
        return 'e.g., 99.99';
      case 'PERCENT_DROP_FROM_LAST':
        return 'e.g., 10';
      default:
        return '';
    }
  }

  private populateForm(): void {
    if (!this.alertRule) return;

    // Find the product for the autocomplete
    const product = this.products.find(p => p.productId === this.alertRule!.canonicalProductId);

    // Determine the condition value based on the condition type
    let conditionValue: number | null = null;
    if (this.alertRule.conditionType === 'PRICE_BELOW') {
      conditionValue = this.alertRule.thresholdValue || null;
    } else if (this.alertRule.conditionType === 'PERCENT_DROP_FROM_LAST') {
      conditionValue = this.alertRule.percentageValue || null;
    }

    this.alertForm.patchValue({
      productId: this.alertRule.canonicalProductId,
      productSearch: product,
      sellerName: this.alertRule.specificSellerName || '',
      alertType: this.alertRule.alertType || 'RECURRING',
      conditionType: this.alertRule.conditionType,
      conditionValue: conditionValue,
      notificationFrequencyMinutes: this.alertRule.notificationFrequencyMinutes,
      ruleDescription: this.alertRule.ruleDescription || '',
      isActive: this.alertRule.isActive
    });
  }

  onSubmit(): void {
    if (this.alertForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    this.isSubmitting = true;
    const formValue = this.alertForm.value;

    let operation: Observable<AlertRuleDto>;

    if (this.isEditMode) {
      // For updates, only include updatable fields
      const updateData = new UpdateAlertRuleDto({
        conditionType: formValue.conditionType,
        alertType: formValue.alertType,
        thresholdValue: formValue.conditionType === 'PRICE_BELOW' ? formValue.conditionValue : undefined,
        percentageValue: formValue.conditionType === 'PERCENT_DROP_FROM_LAST' ? formValue.conditionValue : undefined,
        specificSellerName: formValue.sellerName || undefined,
        notificationFrequencyMinutes: formValue.notificationFrequencyMinutes,
        isActive: formValue.isActive
      });
      operation = this.alertsService.updateAlert(this.alertRule!.alertRuleId!, updateData);
    } else {
      // For creation, include all required fields
      const createData = new CreateAlertRuleDto({
        canonicalProductId: formValue.productId,
        conditionType: formValue.conditionType,
        alertType: formValue.alertType,
        thresholdValue: formValue.conditionType === 'PRICE_BELOW' ? formValue.conditionValue : undefined,
        percentageValue: formValue.conditionType === 'PERCENT_DROP_FROM_LAST' ? formValue.conditionValue : undefined,
        specificSellerName: formValue.sellerName || undefined,
        notificationFrequencyMinutes: formValue.notificationFrequencyMinutes
      });
      operation = this.alertsService.createAlert(createData);
    }

    operation.subscribe({
      next: (result) => {
        this.isSubmitting = false;
        this.snackBar.open(
          this.isEditMode ? 'Alert updated successfully' : 'Alert created successfully',
          'Close',
          { duration: 3000 }
        );
        this.alertSaved.emit(result);
      },
      error: (error) => {
        this.isSubmitting = false;
        console.error('Error saving alert:', error);
        this.snackBar.open(
          'Failed to save alert. Please try again.',
          'Close',
          { duration: 5000 }
        );
      }
    });
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  private markFormGroupTouched(): void {
    Object.keys(this.alertForm.controls).forEach(key => {
      const control = this.alertForm.get(key);
      control?.markAsTouched();
    });
  }
}
