import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

import { AlertsService } from '../../services/alerts.service';
import { AlertTestResultsComponent } from '../alert-test-results/alert-test-results.component';
import { 
  AlertRuleDto, 
  AlertTestResultDto, 
  TestPricePointDto, 
  AlertTestRequestDto 
} from '../../../../shared/api/api-client';

export interface AlertTestDialogData {
  alert: AlertRuleDto;
}

@Component({
  selector: 'app-alert-test-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDividerModule,
    MatChipsModule,
    MatTableModule,
    MatExpansionModule,
    MatTooltipModule,
    MatSlideToggleModule,
    AlertTestResultsComponent
  ],
  template: `
    <div class="alert-test-dialog-container">
      <!-- Header Section -->
      <div class="dialog-header">
        <div class="header-content">
          <div class="title-section">
            <h1 class="mat-headline-large">Test Alert Rule</h1>
            <p class="mat-body-large">{{ data.alert.ruleDescription }}</p>
          </div>
          <button mat-icon-button (click)="close()" class="close-button">
            <mat-icon>close</mat-icon>
          </button>
        </div>
      </div>

      <!-- Main Content -->
      <div class="dialog-content">
        <mat-tab-group class="test-tabs" (selectedTabChange)="onTabChange($event)">
          <!-- Quick Test Tab -->
          <mat-tab label="Quick Test">
            <div class="tab-content">
              <mat-card class="test-card" appearance="outlined">
                <mat-card-header>
                  <mat-card-title>Test with Custom Price</mat-card-title>
                  <mat-card-subtitle>Test your alert against a specific price and stock status</mat-card-subtitle>
                </mat-card-header>
                <mat-card-content>
                  <form [formGroup]="quickTestForm" class="test-form">
                    <div class="form-row">
                      <mat-form-field appearance="outline" class="price-field">
                        <mat-label>Test Price</mat-label>
                        <input matInput type="number" step="0.01" min="0.01" 
                               formControlName="price" placeholder="0.00">
                        <span matTextPrefix>$</span>
                        <mat-error *ngIf="quickTestForm.get('price')?.hasError('required')">
                          Price is required
                        </mat-error>
                        <mat-error *ngIf="quickTestForm.get('price')?.hasError('min')">
                          Price must be greater than $0.01
                        </mat-error>
                      </mat-form-field>

                      <mat-form-field appearance="outline" class="stock-field">
                        <mat-label>Stock Status</mat-label>
                        <mat-select formControlName="stockStatus">
                          <mat-option value="IN_STOCK">In Stock</mat-option>
                          <mat-option value="OUT_OF_STOCK">Out of Stock</mat-option>
                          <mat-option value="LIMITED_STOCK">Limited Stock</mat-option>
                          <mat-option value="PREORDER">Pre-order</mat-option>
                        </mat-select>
                      </mat-form-field>
                    </div>

                    <div class="form-row">
                      <mat-form-field appearance="outline" class="seller-field">
                        <mat-label>Seller Name (Optional)</mat-label>
                        <input matInput formControlName="sellerName" placeholder="Amazon, Best Buy, etc.">
                      </mat-form-field>
                    </div>

                    <div class="test-actions">
                      <button mat-flat-button color="primary" 
                              (click)="runQuickTest()" 
                              [disabled]="quickTestForm.invalid || isLoading"
                              class="test-button">
                        <mat-icon *ngIf="!isLoading">play_arrow</mat-icon>
                        <mat-spinner *ngIf="isLoading" diameter="20"></mat-spinner>
                        {{ isLoading ? 'Testing...' : 'Run Test' }}
                      </button>
                    </div>
                  </form>
                </mat-card-content>
              </mat-card>
            </div>
          </mat-tab>

          <!-- Historical Test Tab -->
          <mat-tab label="Historical Test">
            <div class="tab-content">
              <mat-card class="test-card" appearance="outlined">
                <mat-card-header>
                  <mat-card-title>Test Against Historical Data</mat-card-title>
                  <mat-card-subtitle>See how your alert would have performed with real price history</mat-card-subtitle>
                </mat-card-header>
                <mat-card-content>
                  <form [formGroup]="historicalTestForm" class="test-form">
                    <div class="form-row">
                      <mat-form-field appearance="outline" class="date-field">
                        <mat-label>Start Date</mat-label>
                        <input matInput [matDatepicker]="startPicker" formControlName="startDate">
                        <mat-datepicker-toggle matIconSuffix [for]="startPicker"></mat-datepicker-toggle>
                        <mat-datepicker #startPicker></mat-datepicker>
                      </mat-form-field>

                      <mat-form-field appearance="outline" class="date-field">
                        <mat-label>End Date</mat-label>
                        <input matInput [matDatepicker]="endPicker" formControlName="endDate">
                        <mat-datepicker-toggle matIconSuffix [for]="endPicker"></mat-datepicker-toggle>
                        <mat-datepicker #endPicker></mat-datepicker>
                      </mat-form-field>
                    </div>

                    <div class="form-row">
                      <mat-form-field appearance="outline" class="records-field">
                        <mat-label>Max Records</mat-label>
                        <input matInput type="number" min="10" max="1000" 
                               formControlName="maxRecords" placeholder="100">
                        <mat-hint>Limit the number of historical records to analyze</mat-hint>
                      </mat-form-field>
                    </div>

                    <div class="test-actions">
                      <button mat-flat-button color="primary" 
                              (click)="runHistoricalTest()" 
                              [disabled]="historicalTestForm.invalid || isLoading"
                              class="test-button">
                        <mat-icon *ngIf="!isLoading">history</mat-icon>
                        <mat-spinner *ngIf="isLoading" diameter="20"></mat-spinner>
                        {{ isLoading ? 'Analyzing...' : 'Analyze History' }}
                      </button>
                    </div>
                  </form>
                </mat-card-content>
              </mat-card>
            </div>
          </mat-tab>
        </mat-tab-group>

        <!-- Test Results Section -->
        <div class="results-section" *ngIf="testResults">
          <app-alert-test-results 
            [results]="testResults" 
            [alert]="data.alert">
          </app-alert-test-results>
        </div>
      </div>

      <!-- Footer Actions -->
      <div class="dialog-footer">
        <div class="footer-actions">
          <button mat-button (click)="close()">Close</button>
          <button mat-flat-button color="accent" (click)="exportResults()" 
                  [disabled]="!testResults">
            <mat-icon>download</mat-icon>
            Export Results
          </button>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./alert-test-dialog.component.scss']
})
export class AlertTestDialogComponent implements OnInit {
  quickTestForm: FormGroup;
  historicalTestForm: FormGroup;
  isLoading = false;
  testResults: AlertTestResultDto | null = null;
  selectedTabIndex = 0;
  sendNotifications = false;

  constructor(
    public dialogRef: MatDialogRef<AlertTestDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: AlertTestDialogData,
    private fb: FormBuilder,
    private alertsService: AlertsService,
    private snackBar: MatSnackBar
  ) {
    this.quickTestForm = this.createQuickTestForm();
    this.historicalTestForm = this.createHistoricalTestForm();
  }

  ngOnInit(): void {
    // Set default date range for historical test (last 30 days)
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - 30);
    
    this.historicalTestForm.patchValue({
      startDate: startDate,
      endDate: endDate,
      maxRecords: 100
    });
  }

  private createQuickTestForm(): FormGroup {
    return this.fb.group({
      price: [null, [Validators.required, Validators.min(0.01)]],
      stockStatus: ['IN_STOCK', Validators.required],
      sellerName: ['']
    });
  }

  private createHistoricalTestForm(): FormGroup {
    return this.fb.group({
      startDate: [null],
      endDate: [null],
      maxRecords: [100, [Validators.min(10), Validators.max(1000)]]
    });
  }

  onTabChange(event: any): void {
    this.selectedTabIndex = event.index;
    this.testResults = null; // Clear results when switching tabs
  }

  async runQuickTest(): Promise<void> {
    if (this.quickTestForm.invalid) return;

    this.isLoading = true;
    this.testResults = null;

    try {
      const formValue = this.quickTestForm.value;
      const testPricePoint = new TestPricePointDto({
        price: formValue.price,
        stockStatus: formValue.stockStatus,
        sellerName: formValue.sellerName || undefined,
        sourceUrl: 'test://manual-test',
        timestamp: new Date(),
        sendNotification: this.sendNotifications
      });

      this.testResults = await this.alertsService.testAlert(
        this.data.alert.alertRuleId!, 
        testPricePoint
      ).toPromise() || null;

      this.snackBar.open('Test completed successfully', 'Close', { duration: 3000 });
    } catch (error: any) {
      console.error('Error running quick test:', error);
      this.snackBar.open(
        error.message || 'Failed to run test', 
        'Close', 
        { duration: 5000 }
      );
    } finally {
      this.isLoading = false;
    }
  }

  async runHistoricalTest(): Promise<void> {
    this.isLoading = true;
    this.testResults = null;

    try {
      const formValue = this.historicalTestForm.value;
      const request = new AlertTestRequestDto({
        alertRuleId: this.data.alert.alertRuleId!,
        startDate: formValue.startDate,
        endDate: formValue.endDate,
        maxRecords: formValue.maxRecords,
        sendNotification: this.sendNotifications
      });

      this.testResults = await this.alertsService.testAlertAgainstHistory(request).toPromise() || null;

      this.snackBar.open('Historical analysis completed', 'Close', { duration: 3000 });
    } catch (error: any) {
      console.error('Error running historical test:', error);
      this.snackBar.open(
        error.message || 'Failed to analyze historical data', 
        'Close', 
        { duration: 5000 }
      );
    } finally {
      this.isLoading = false;
    }
  }

  exportResults(): void {
    if (!this.testResults) return;

    const dataStr = JSON.stringify(this.testResults, null, 2);
    const dataBlob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(dataBlob);
    
    const link = document.createElement('a');
    link.href = url;
    link.download = `alert-test-results-${this.data.alert.alertRuleId}-${new Date().toISOString().split('T')[0]}.json`;
    link.click();
    
    URL.revokeObjectURL(url);
    this.snackBar.open('Results exported successfully', 'Close', { duration: 2000 });
  }

  close(): void {
    this.dialogRef.close();
  }
} 