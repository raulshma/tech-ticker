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
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
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
    MatAutocompleteModule,
    MatDividerModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="alert-form-container">
      <!-- Modern Header Section -->
      <header class="form-header">
        <div class="header-content">
          <div class="title-section">
            <h1 class="mat-display-small">{{ isEditMode ? 'Edit Alert Rule' : 'Create Alert Rule' }}</h1>
            <p class="mat-body-large form-subtitle">{{ isEditMode ? 'Update your price and availability notification settings' : 'Set up price and availability notifications for your favorite products' }}</p>
          </div>
          <div class="header-icon">
            <mat-icon>{{ isEditMode ? 'edit_notifications' : 'add_alert' }}</mat-icon>
          </div>
        </div>
      </header>

      <!-- Modern Form Section -->
      <section class="form-section">
        <mat-card class="form-card" appearance="outlined">
          <mat-card-content class="form-content">
            <form [formGroup]="alertForm" (ngSubmit)="onSubmit()">
              <!-- Product Selection Section -->
              <div class="form-section-header">
                <mat-icon class="section-icon">inventory_2</mat-icon>
                <div class="section-title">
                  <h3 class="mat-title-large">Product Selection</h3>
                  <p class="mat-body-medium">Choose the product you want to monitor</p>
                </div>
              </div>
              
              <div class="form-fields-group">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Product</mat-label>
                  <input
                    type="text"
                    matInput
                    formControlName="productSearch"
                    [matAutocomplete]="productAuto"
                    placeholder="Search for a product..."
                    required>
                  <mat-icon matSuffix>search</mat-icon>
                  <mat-autocomplete #productAuto="matAutocomplete" [displayWith]="displayProduct">
                    <mat-option
                      *ngFor="let product of filteredProducts | async"
                      [value]="product"
                      (onSelectionChange)="onProductSelected(product)">
                      <div class="product-option">
                        <div class="product-option-main">
                          <div class="product-name">{{ product.name }}</div>
                          <div class="product-details">{{ product.manufacturer }} • {{ product.category?.name }}</div>
                        </div>
                      </div>
                    </mat-option>
                  </mat-autocomplete>
                  <mat-error *ngIf="alertForm.get('productId')?.hasError('required')">
                    Product selection is required
                  </mat-error>
                  <mat-hint>Select a product from the dropdown to monitor</mat-hint>
                </mat-form-field>

                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Specific Seller (Optional)</mat-label>
                  <input matInput formControlName="sellerName" placeholder="e.g., Amazon, Best Buy, etc.">
                  <mat-icon matSuffix>store</mat-icon>
                  <mat-hint>Leave empty to monitor all sellers for this product</mat-hint>
                </mat-form-field>
              </div>

              <mat-divider class="section-divider"></mat-divider>

              <!-- Alert Conditions Section -->
              <div class="form-section-header">
                <mat-icon class="section-icon">tune</mat-icon>
                <div class="section-title">
                  <h3 class="mat-title-large">Alert Conditions</h3>
                  <p class="mat-body-medium">Define when you want to be notified</p>
                </div>
              </div>

              <div class="form-fields-group">
                <div class="field-row">
                  <mat-form-field appearance="outline" class="half-width">
                    <mat-label>Alert Type</mat-label>
                    <mat-select formControlName="alertType" required>
                      <mat-option value="RECURRING">
                        <div class="select-option">
                          <mat-icon>repeat</mat-icon>
                          <span>Recurring</span>
                        </div>
                      </mat-option>
                      <mat-option value="ONE_SHOT">
                        <div class="select-option">
                          <mat-icon>play_arrow</mat-icon>
                          <span>One-time</span>
                        </div>
                      </mat-option>
                    </mat-select>
                    <mat-hint>Recurring alerts continue until disabled</mat-hint>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="half-width">
                    <mat-label>Condition Type</mat-label>
                    <mat-select formControlName="conditionType" (selectionChange)="onConditionTypeChange()" required>
                      <mat-option value="PRICE_BELOW">
                        <div class="select-option">
                          <mat-icon>trending_down</mat-icon>
                          <span>Price drops below</span>
                        </div>
                      </mat-option>
                      <mat-option value="PERCENT_DROP_FROM_LAST">
                        <div class="select-option">
                          <mat-icon>percent</mat-icon>
                          <span>Percentage drop</span>
                        </div>
                      </mat-option>
                      <mat-option value="BACK_IN_STOCK">
                        <div class="select-option">
                          <mat-icon>inventory</mat-icon>
                          <span>Back in stock</span>
                        </div>
                      </mat-option>
                    </mat-select>
                  </mat-form-field>
                </div>

                <!-- Condition Value Field -->
                <mat-form-field
                  appearance="outline"
                  class="full-width"
                  *ngIf="showConditionValue()">
                  <mat-label>{{ getConditionValueLabel() }}</mat-label>
                  <input
                    matInput
                    type="number"
                    formControlName="conditionValue"
                    [placeholder]="getConditionValuePlaceholder()"
                    required>
                  <span matSuffix *ngIf="alertForm.get('conditionType')?.value === 'PRICE_BELOW'">USD</span>
                  <span matSuffix *ngIf="alertForm.get('conditionType')?.value === 'PERCENT_DROP_FROM_LAST'">%</span>
                  <mat-icon matPrefix>{{ getConditionValueIcon() }}</mat-icon>
                  <mat-error *ngIf="alertForm.get('conditionValue')?.hasError('required')">
                    {{ getConditionValueLabel() }} is required
                  </mat-error>
                  <mat-error *ngIf="alertForm.get('conditionValue')?.hasError('min')">
                    Value must be greater than 0
                  </mat-error>
                  <mat-hint>{{ getConditionValueHint() }}</mat-hint>
                </mat-form-field>
              </div>

              <mat-divider class="section-divider"></mat-divider>

              <!-- Notification Settings Section -->
              <div class="form-section-header">
                <mat-icon class="section-icon">notifications</mat-icon>
                <div class="section-title">
                  <h3 class="mat-title-large">Notification Settings</h3>
                  <p class="mat-body-medium">Control how often you receive notifications</p>
                </div>
              </div>

              <div class="form-fields-group">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Notification Frequency</mat-label>
                  <mat-select formControlName="notificationFrequencyMinutes" required>
                    <mat-option [value]="0">
                      <div class="select-option">
                        <mat-icon>flash_on</mat-icon>
                        <span>Immediate</span>
                      </div>
                    </mat-option>
                    <mat-option [value]="15">
                      <div class="select-option">
                        <mat-icon>access_time</mat-icon>
                        <span>Every 15 minutes</span>
                      </div>
                    </mat-option>
                    <mat-option [value]="30">
                      <div class="select-option">
                        <mat-icon>schedule</mat-icon>
                        <span>Every 30 minutes</span>
                      </div>
                    </mat-option>
                    <mat-option [value]="60">
                      <div class="select-option">
                        <mat-icon>schedule</mat-icon>
                        <span>Every hour</span>
                      </div>
                    </mat-option>
                    <mat-option [value]="240">
                      <div class="select-option">
                        <mat-icon>today</mat-icon>
                        <span>Every 4 hours</span>
                      </div>
                    </mat-option>
                    <mat-option [value]="1440">
                      <div class="select-option">
                        <mat-icon>today</mat-icon>
                        <span>Daily</span>
                      </div>
                    </mat-option>
                  </mat-select>
                  <mat-hint>Minimum time between notifications for the same alert</mat-hint>
                </mat-form-field>

                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Description (Optional)</mat-label>
                  <textarea
                    matInput
                    formControlName="ruleDescription"
                    rows="3"
                    placeholder="Add a custom description for this alert rule...">
                  </textarea>
                  <mat-icon matSuffix>description</mat-icon>
                  <mat-hint>Optional: Add notes to help you identify this alert</mat-hint>
                </mat-form-field>

                <!-- Active Toggle -->
                <div class="toggle-section">
                  <div class="toggle-header">
                    <mat-icon class="toggle-icon">power_settings_new</mat-icon>
                    <div class="toggle-info">
                      <h4 class="mat-title-medium">Alert Status</h4>
                      <p class="mat-body-small">Enable or disable this alert rule</p>
                    </div>
                  </div>
                  <mat-slide-toggle formControlName="isActive" color="primary">
                    {{ alertForm.get('isActive')?.value ? 'Active' : 'Inactive' }}
                  </mat-slide-toggle>
                </div>
              </div>
            </form>
          </mat-card-content>

          <!-- Enhanced Actions Section -->
          <mat-card-actions class="form-actions">
            <div class="actions-content">
              <div class="actions-left">
                <button matButton="outlined" type="button" (click)="onCancel()" [disabled]="isSubmitting">
                  <mat-icon>cancel</mat-icon>
                  Cancel
                </button>
              </div>
              <div class="actions-right">
                <button matButton="outlined" type="button" (click)="previewAlert()" [disabled]="alertForm.invalid || isSubmitting">
                  <mat-icon>preview</mat-icon>
                  Preview
                </button>
                <button
                  matButton="filled"
                  color="primary"
                  (click)="onSubmit()"
                  [disabled]="alertForm.invalid || isSubmitting">
                  <mat-spinner diameter="20" *ngIf="isSubmitting" class="button-spinner"></mat-spinner>
                  <mat-icon *ngIf="!isSubmitting">{{ isEditMode ? 'save' : 'add_alert' }}</mat-icon>
                  {{ isSubmitting ? 'Saving...' : (isEditMode ? 'Update Alert' : 'Create Alert') }}
                </button>
              </div>
            </div>
          </mat-card-actions>
        </mat-card>
      </section>
    </div>
  `,
  styleUrls: ['./alert-form.component.scss']
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

  getConditionValueIcon(): string {
    const conditionType = this.alertForm.get('conditionType')?.value;
    switch (conditionType) {
      case 'PRICE_BELOW':
        return 'attach_money';
      case 'PERCENT_DROP_FROM_LAST':
        return 'percent';
      default:
        return 'help';
    }
  }

  getConditionValueHint(): string {
    const conditionType = this.alertForm.get('conditionType')?.value;
    switch (conditionType) {
      case 'PRICE_BELOW':
        return 'Alert will trigger when price drops below this amount';
      case 'PERCENT_DROP_FROM_LAST':
        return 'Alert will trigger when price drops by this percentage';
      default:
        return '';
    }
  }

  previewAlert(): void {
    if (this.alertForm.valid) {
      const formValue = this.alertForm.value;
      let previewText = `Alert Preview:\n\n`;
      previewText += `Product: ${formValue.productSearch?.name || 'Selected Product'}\n`;
      if (formValue.sellerName) {
        previewText += `Seller: ${formValue.sellerName}\n`;
      }
      previewText += `Type: ${formValue.alertType}\n`;
      previewText += `Condition: ${this.getConditionText(formValue)}\n`;
      previewText += `Frequency: ${this.getFrequencyText(formValue.notificationFrequencyMinutes)}\n`;
      previewText += `Status: ${formValue.isActive ? 'Active' : 'Inactive'}`;
      
      alert(previewText);
    }
  }

  private getConditionText(formValue: any): string {
    switch (formValue.conditionType) {
      case 'PRICE_BELOW':
        return `Price drops below $${formValue.conditionValue}`;
      case 'PERCENT_DROP_FROM_LAST':
        return `Price drops by ${formValue.conditionValue}%`;
      case 'BACK_IN_STOCK':
        return 'Product comes back in stock';
      default:
        return 'Unknown condition';
    }
  }

  private getFrequencyText(minutes: number): string {
    if (minutes === 0) return 'Immediate';
    if (minutes < 60) return `Every ${minutes} minutes`;
    if (minutes < 1440) return `Every ${minutes / 60} hour${minutes / 60 > 1 ? 's' : ''}`;
    return `Every ${minutes / 1440} day${minutes / 1440 > 1 ? 's' : ''}`;
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
        notificationFrequencyMinutes: formValue.notificationFrequencyMinutes,
        isActive: formValue.isActive,
        specificSellerName: formValue.sellerName || undefined
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
        notificationFrequencyMinutes: formValue.notificationFrequencyMinutes,
        specificSellerName: formValue.sellerName || undefined
      });

      operation = this.alertsService.createAlert(createData);
    }

    operation.subscribe({
      next: (savedAlert) => {
        this.isSubmitting = false;
        this.snackBar.open(
          `Alert ${this.isEditMode ? 'updated' : 'created'} successfully`,
          'Close',
          { duration: 3000 }
        );
        this.alertSaved.emit(savedAlert);
      },
      error: (error) => {
        console.error('Error saving alert:', error);
        this.isSubmitting = false;
        this.snackBar.open(
          `Failed to ${this.isEditMode ? 'update' : 'create'} alert`,
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
