import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';

import { AlertsService } from '../../services/alerts.service';
import { AlertRuleDto, UpdateAlertRuleDto } from '../../../../shared/api/api-client';

@Component({
  selector: 'app-alert-admin',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatSlideToggleModule,
    MatMenuModule,
    MatSnackBarModule,
    ReactiveFormsModule
  ],
  template: `
    <div class="admin-container">
      <mat-card class="header-card">
        <mat-card-header>
          <mat-card-title>Alert Administration</mat-card-title>
          <mat-card-subtitle>Manage all user alert rules</mat-card-subtitle>
        </mat-card-header>
        <mat-card-actions>
          <button mat-raised-button color="primary" (click)="refreshData()">
            <mat-icon>refresh</mat-icon>
            Refresh
          </button>
          <button mat-raised-button color="accent" routerLink="/alerts/performance">
            <mat-icon>analytics</mat-icon>
            Performance
          </button>
        </mat-card-actions>
      </mat-card>

      <!-- Filters -->
      <mat-card class="filters-card">
        <mat-card-content>
          <div class="filters-row">
            <mat-form-field appearance="outline">
              <mat-label>Search</mat-label>
              <input matInput [formControl]="searchControl" placeholder="Search by product name or user...">
              <mat-icon matSuffix>search</mat-icon>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Status</mat-label>
              <mat-select [formControl]="statusFilter">
                <mat-option value="">All</mat-option>
                <mat-option value="active">Active</mat-option>
                <mat-option value="inactive">Inactive</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Condition Type</mat-label>
              <mat-select [formControl]="conditionFilter">
                <mat-option value="">All</mat-option>
                <mat-option value="PRICE_BELOW">Price Below</mat-option>
                <mat-option value="PERCENT_DROP_FROM_LAST">Percentage Drop</mat-option>
                <mat-option value="BACK_IN_STOCK">Back in Stock</mat-option>
              </mat-select>
            </mat-form-field>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- Alerts Table -->
      <mat-card class="table-card">
        <mat-card-content>
          <div class="table-header">
            <h3>Alert Rules ({{ totalAlerts }})</h3>
          </div>

          <table mat-table [dataSource]="alerts" class="alerts-table">
            <!-- User Column -->
            <ng-container matColumnDef="user">
              <th mat-header-cell *matHeaderCellDef>User</th>
              <td mat-cell *matCellDef="let alert">
                <div class="user-info">
                  <div class="user-name">{{ alert.user?.firstName }} {{ alert.user?.lastName }}</div>
                  <div class="user-email">{{ alert.user?.email }}</div>
                </div>
              </td>
            </ng-container>

            <!-- Product Column -->
            <ng-container matColumnDef="product">
              <th mat-header-cell *matHeaderCellDef>Product</th>
              <td mat-cell *matCellDef="let alert">
                <div class="product-info">
                  <div class="product-name">{{ alert.product?.name }}</div>
                  <div class="product-manufacturer">{{ alert.product?.manufacturer }}</div>
                  <div class="seller-name" *ngIf="alert.specificSellerName">
                    Seller: {{ alert.specificSellerName }}
                  </div>
                </div>
              </td>
            </ng-container>

            <!-- Condition Column -->
            <ng-container matColumnDef="condition">
              <th mat-header-cell *matHeaderCellDef>Condition</th>
              <td mat-cell *matCellDef="let alert">
                <div class="condition-info">
                  <mat-chip [color]="getConditionColor(alert.conditionType)">
                    {{ getConditionText(alert) }}
                  </mat-chip>
                  <div class="alert-type">{{ alert.alertType }}</div>
                </div>
              </td>
            </ng-container>

            <!-- Status Column -->
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let alert">
                <div class="status-info">
                  <mat-slide-toggle 
                    [checked]="alert.isActive"
                    (change)="toggleAlert(alert, $event.checked)"
                    [disabled]="isUpdating">
                  </mat-slide-toggle>
                  <span class="status-text">
                    {{ alert.isActive ? 'Active' : 'Inactive' }}
                  </span>
                </div>
              </td>
            </ng-container>

            <!-- Created Column -->
            <ng-container matColumnDef="created">
              <th mat-header-cell *matHeaderCellDef>Created</th>
              <td mat-cell *matCellDef="let alert">
                <div class="created-info">
                  {{ alert.createdAt | date:'short' }}
                </div>
              </td>
            </ng-container>

            <!-- Last Triggered Column -->
            <ng-container matColumnDef="lastTriggered">
              <th mat-header-cell *matHeaderCellDef>Last Triggered</th>
              <td mat-cell *matCellDef="let alert">
                <div class="last-triggered">
                  {{ alert.lastNotifiedAt ? (alert.lastNotifiedAt | date:'short') : 'Never' }}
                </div>
              </td>
            </ng-container>

            <!-- Actions Column -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let alert">
                <button mat-icon-button [matMenuTriggerFor]="actionsMenu">
                  <mat-icon>more_vert</mat-icon>
                </button>
                <mat-menu #actionsMenu="matMenu">
                  <button mat-menu-item (click)="viewAlert(alert)">
                    <mat-icon>visibility</mat-icon>
                    <span>View Details</span>
                  </button>
                  <button mat-menu-item (click)="editAlert(alert)">
                    <mat-icon>edit</mat-icon>
                    <span>Edit</span>
                  </button>
                  <button mat-menu-item (click)="deleteAlert(alert)" class="delete-action">
                    <mat-icon>delete</mat-icon>
                    <span>Delete</span>
                  </button>
                </mat-menu>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>

          <!-- Pagination -->
          <mat-paginator
            [length]="totalAlerts"
            [pageSize]="pageSize"
            [pageSizeOptions]="[10, 25, 50, 100]"
            [pageIndex]="currentPage"
            (page)="onPageChange($event)"
            showFirstLastButtons>
          </mat-paginator>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .admin-container {
      padding: 20px;
      max-width: 1400px;
      margin: 0 auto;
    }

    .header-card, .filters-card, .table-card {
      margin-bottom: 20px;
    }

    .filters-row {
      display: grid;
      grid-template-columns: 2fr 1fr 1fr;
      gap: 16px;
      align-items: center;
    }

    .table-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
    }

    .alerts-table {
      width: 100%;
    }

    .user-info, .product-info, .condition-info, .status-info {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .user-name, .product-name {
      font-weight: 500;
    }

    .user-email, .product-manufacturer, .seller-name, .alert-type {
      font-size: 12px;
      color: #666;
    }

    .status-info {
      flex-direction: row;
      align-items: center;
      gap: 8px;
    }

    .delete-action {
      color: #f44336;
    }

    .created-info, .last-triggered {
      font-size: 12px;
    }

    @media (max-width: 768px) {
      .filters-row {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class AlertAdminComponent implements OnInit {
  alerts: AlertRuleDto[] = [];
  displayedColumns: string[] = ['user', 'product', 'condition', 'status', 'created', 'lastTriggered', 'actions'];
  
  // Pagination
  totalAlerts = 0;
  pageSize = 25;
  currentPage = 0;
  
  // Filters
  searchControl = new FormControl('');
  statusFilter = new FormControl('');
  conditionFilter = new FormControl('');
  
  // State
  isLoading = false;
  isUpdating = false;

  constructor(
    private alertsService: AlertsService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.setupFilters();
    this.loadAlerts();
  }

  private setupFilters(): void {
    // Setup search debouncing
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(() => {
      this.currentPage = 0;
      this.loadAlerts();
    });

    // Setup filter changes
    this.statusFilter.valueChanges.subscribe(() => {
      this.currentPage = 0;
      this.loadAlerts();
    });

    this.conditionFilter.valueChanges.subscribe(() => {
      this.currentPage = 0;
      this.loadAlerts();
    });
  }

  loadAlerts(): void {
    this.isLoading = true;
    
    // For now, we'll use the regular getUserAlerts method
    // In a real implementation, you'd call the admin endpoint with filters
    this.alertsService.getUserAlerts().subscribe({
      next: (alerts) => {
        this.alerts = this.applyClientSideFilters(alerts);
        this.totalAlerts = this.alerts.length;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading alerts:', error);
        this.snackBar.open('Failed to load alerts', 'Close', { duration: 3000 });
        this.isLoading = false;
      }
    });
  }

  private applyClientSideFilters(alerts: AlertRuleDto[]): AlertRuleDto[] {
    let filtered = alerts;

    // Apply search filter
    const searchTerm = this.searchControl.value?.toLowerCase();
    if (searchTerm) {
      filtered = filtered.filter(alert => 
        alert.product?.name?.toLowerCase().includes(searchTerm) ||
        alert.user?.email?.toLowerCase().includes(searchTerm) ||
        alert.user?.firstName?.toLowerCase().includes(searchTerm) ||
        alert.user?.lastName?.toLowerCase().includes(searchTerm)
      );
    }

    // Apply status filter
    const statusFilter = this.statusFilter.value;
    if (statusFilter === 'active') {
      filtered = filtered.filter(alert => alert.isActive);
    } else if (statusFilter === 'inactive') {
      filtered = filtered.filter(alert => !alert.isActive);
    }

    // Apply condition filter
    const conditionFilter = this.conditionFilter.value;
    if (conditionFilter) {
      filtered = filtered.filter(alert => alert.conditionType === conditionFilter);
    }

    return filtered;
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadAlerts();
  }

  refreshData(): void {
    this.loadAlerts();
    this.snackBar.open('Data refreshed', 'Close', { duration: 2000 });
  }

  toggleAlert(alert: AlertRuleDto, isActive: boolean): void {
    this.isUpdating = true;
    const updateDto = new UpdateAlertRuleDto({ isActive });
    
    this.alertsService.updateAlert(alert.alertRuleId!, updateDto).subscribe({
      next: (updatedAlert) => {
        const index = this.alerts.findIndex(a => a.alertRuleId === alert.alertRuleId);
        if (index !== -1) {
          this.alerts[index] = updatedAlert;
        }
        this.isUpdating = false;
        this.snackBar.open(
          `Alert ${isActive ? 'activated' : 'deactivated'}`,
          'Close',
          { duration: 2000 }
        );
      },
      error: (error) => {
        console.error('Error updating alert:', error);
        this.snackBar.open('Failed to update alert', 'Close', { duration: 3000 });
        this.isUpdating = false;
        // Revert the toggle
        alert.isActive = !isActive;
      }
    });
  }

  viewAlert(alert: AlertRuleDto): void {
    // TODO: Implement alert details view
    this.snackBar.open('Alert details view coming soon', 'Close', { duration: 2000 });
  }

  editAlert(alert: AlertRuleDto): void {
    // TODO: Navigate to edit form or open modal
    this.snackBar.open('Alert editing coming soon', 'Close', { duration: 2000 });
  }

  deleteAlert(alert: AlertRuleDto): void {
    if (confirm(`Are you sure you want to delete the alert for "${alert.product?.name}"?`)) {
      this.alertsService.deleteAlert(alert.alertRuleId!).subscribe({
        next: () => {
          this.alerts = this.alerts.filter(a => a.alertRuleId !== alert.alertRuleId);
          this.totalAlerts--;
          this.snackBar.open('Alert deleted successfully', 'Close', { duration: 2000 });
        },
        error: (error) => {
          console.error('Error deleting alert:', error);
          this.snackBar.open('Failed to delete alert', 'Close', { duration: 3000 });
        }
      });
    }
  }

  getConditionText(alert: AlertRuleDto): string {
    switch (alert.conditionType) {
      case 'PRICE_BELOW':
        return `Price below $${alert.thresholdValue}`;
      case 'PERCENT_DROP_FROM_LAST':
        return `${alert.percentageValue}% drop`;
      case 'BACK_IN_STOCK':
        return 'Back in stock';
      default:
        return alert.conditionType || 'Unknown';
    }
  }

  getConditionColor(conditionType: string): string {
    switch (conditionType) {
      case 'PRICE_BELOW':
        return 'primary';
      case 'PERCENT_DROP_FROM_LAST':
        return 'accent';
      case 'BACK_IN_STOCK':
        return 'warn';
      default:
        return '';
    }
  }
}
