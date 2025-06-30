import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';

import { AlertsService } from '../../services/alerts.service';
import { AlertRuleDto, UpdateAlertRuleDto } from '../../../../shared/api/api-client';

@Component({
  selector: 'app-alerts-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatChipsModule,
    MatSlideToggleModule,
    MatMenuModule,
    MatSnackBarModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatDividerModule
  ],
  template: `
    <div class="alerts-list-container">
      <!-- Modern Welcome Header Section -->
      <header class="welcome-section">
        <div class="welcome-content">
          <div class="header-main">
            <div class="title-section">
              <h1 class="mat-display-medium">Alert Rules</h1>
              <p class="mat-body-large welcome-subtitle">Manage your price and availability notifications</p>
            </div>
            <div class="header-actions">
              <button matButton="filled" color="primary" (click)="createAlert()">
                <mat-icon>add</mat-icon>
                Create Alert
              </button>
              <button matButton="filled" color="accent" (click)="viewPerformance()">
                <mat-icon>analytics</mat-icon>
                Performance
              </button>
              <button matButton="outlined" (click)="loadAlerts()" [disabled]="isLoading">
                <mat-icon>refresh</mat-icon>
                Refresh
              </button>
            </div>
          </div>
        </div>
      </header>

      <!-- Enhanced Statistics Overview -->
      <section class="stats-section" *ngIf="!isLoading && alerts.length > 0" aria-label="Alert Statistics">
        <div class="stats-grid">
          <mat-card class="stat-card total-alerts" appearance="outlined" tabindex="0">
            <mat-card-content>
              <div class="stat-content">
                <div class="stat-icon primary-surface">
                  <mat-icon aria-hidden="true">notifications</mat-icon>
                </div>
                <div class="stat-info">
                  <h3 class="mat-headline-medium stat-number">{{ alerts.length }}</h3>
                  <p class="mat-body-medium stat-label">Total Alerts</p>
                </div>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="stat-card active-alerts" appearance="outlined" tabindex="0">
            <mat-card-content>
              <div class="stat-content">
                <div class="stat-icon success-surface">
                  <mat-icon aria-hidden="true">notifications_active</mat-icon>
                </div>
                <div class="stat-info">
                  <h3 class="mat-headline-medium stat-number">{{ getActiveAlertsCount() }}</h3>
                  <p class="mat-body-medium stat-label">Active</p>
                </div>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="stat-card recent-triggers" appearance="outlined" tabindex="0">
            <mat-card-content>
              <div class="stat-content">
                <div class="stat-icon secondary-surface">
                  <mat-icon aria-hidden="true">schedule</mat-icon>
                </div>
                <div class="stat-info">
                  <h3 class="mat-headline-medium stat-number">{{ getRecentTriggersCount() }}</h3>
                  <p class="mat-body-medium stat-label">Recent Triggers</p>
                </div>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="stat-card condition-types" appearance="outlined" tabindex="0">
            <mat-card-content>
              <div class="stat-content">
                <div class="stat-icon info-surface">
                  <mat-icon aria-hidden="true">category</mat-icon>
                </div>
                <div class="stat-info">
                  <h3 class="mat-headline-medium stat-number">{{ getUniqueConditionTypesCount() }}</h3>
                  <p class="mat-body-medium stat-label">Condition Types</p>
                </div>
              </div>
            </mat-card-content>
          </mat-card>
        </div>
      </section>

      <!-- Modern Management Section -->
      <section class="management-section" aria-label="Alert Management">
        <mat-card class="management-card" appearance="outlined">
          <mat-card-header class="management-header">
            <mat-card-title class="mat-headline-large">Your Alert Rules</mat-card-title>
            <mat-card-subtitle class="mat-body-large">Configure and monitor your personalized price alerts</mat-card-subtitle>
          </mat-card-header>

          <mat-card-content class="management-content">
            <!-- Loading State -->
            <div *ngIf="isLoading" class="loading-container" role="status" aria-live="polite">
              <mat-spinner diameter="48" strokeWidth="4"></mat-spinner>
              <p class="mat-body-large loading-text">Loading your alert rules...</p>
            </div>

            <!-- Enhanced Empty State -->
            <div class="empty-state-container" *ngIf="!isLoading && alerts.length === 0">
              <div class="empty-state-content">
                <mat-icon class="empty-state-icon">notifications_none</mat-icon>
                <h3 class="mat-headline-small">No Alert Rules Yet</h3>
                <p class="mat-body-medium">
                  Create your first alert rule to get notified about price changes and product availability.
                </p>
                <div class="empty-state-actions">
                  <button matButton="filled" color="primary" (click)="createAlert()">
                    <mat-icon>add</mat-icon>
                    Create Your First Alert
                  </button>
                  <button matButton="outlined" routerLink="/catalog">
                    <mat-icon>search</mat-icon>
                    Browse Products
                  </button>
                </div>
                <div class="empty-state-features">
                  <div class="feature-item">
                    <mat-icon>trending_down</mat-icon>
                    <span>Price drop alerts</span>
                  </div>
                  <div class="feature-item">
                    <mat-icon>inventory</mat-icon>
                    <span>Back in stock notifications</span>
                  </div>
                  <div class="feature-item">
                    <mat-icon>percent</mat-icon>
                    <span>Percentage-based alerts</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- Enhanced Alerts Table -->
            <div class="table-container" *ngIf="!isLoading && alerts.length > 0">
              <div class="table-wrapper">
                <table mat-table [dataSource]="alerts" class="modern-table">
                  <!-- Product Column -->
                  <ng-container matColumnDef="product">
                    <th mat-header-cell *matHeaderCellDef class="mat-title-medium product-header">Product</th>
                    <td mat-cell *matCellDef="let alert" class="product-cell">
                      <div class="product-info-modern">
                        <div class="product-avatar">
                          <mat-icon>inventory_2</mat-icon>
                        </div>
                        <div class="product-details">
                          <div class="product-name mat-body-medium">{{ alert.product?.name }}</div>
                          <div class="product-brand mat-body-small">{{ alert.product?.manufacturer }}</div>
                          <div class="seller-name mat-body-small" *ngIf="alert.specificSellerName">
                            <mat-icon class="seller-icon">store</mat-icon>
                            {{ alert.specificSellerName }}
                          </div>
                        </div>
                      </div>
                    </td>
                  </ng-container>

                  <!-- Condition Column -->
                  <ng-container matColumnDef="condition">
                    <th mat-header-cell *matHeaderCellDef class="mat-title-medium condition-header">Condition</th>
                    <td mat-cell *matCellDef="let alert" class="condition-cell">
                      <div class="condition-info-modern">
                        <mat-chip [class]="'condition-chip-' + getConditionChipClass(alert.conditionType)" class="condition-chip">
                          <mat-icon>{{ getConditionIcon(alert.conditionType) }}</mat-icon>
                          {{ getConditionText(alert) }}
                        </mat-chip>
                        <div class="alert-type-modern mat-body-small">
                          <mat-icon class="type-icon">{{ getAlertTypeIcon(alert.alertType) }}</mat-icon>
                          {{ alert.alertType }}
                        </div>
                      </div>
                    </td>
                  </ng-container>

                  <!-- Status Column -->
                  <ng-container matColumnDef="status">
                    <th mat-header-cell *matHeaderCellDef class="mat-title-medium status-header">Status</th>
                    <td mat-cell *matCellDef="let alert" class="status-cell">
                      <div class="status-container-modern">
                        <div class="status-toggle">
                          <mat-slide-toggle 
                            [checked]="alert.isActive"
                            (change)="toggleAlert(alert, $event.checked)"
                            [disabled]="isUpdating"
                            [color]="'primary'">
                          </mat-slide-toggle>
                          <span class="status-label mat-body-small">{{ alert.isActive ? 'Active' : 'Inactive' }}</span>
                        </div>
                        <div class="status-details mat-body-small">
                          <mat-icon class="notification-icon">notifications</mat-icon>
                          Every {{ alert.notificationFrequencyMinutes || 0 }}min
                        </div>
                      </div>
                    </td>
                  </ng-container>

                  <!-- Last Triggered Column -->
                  <ng-container matColumnDef="lastTriggered">
                    <th mat-header-cell *matHeaderCellDef class="mat-title-medium triggered-header">Last Triggered</th>
                    <td mat-cell *matCellDef="let alert" class="triggered-cell">
                      <div class="triggered-info-modern" *ngIf="alert.lastNotifiedAt; else neverTriggered">
                        <div class="triggered-date mat-body-medium">{{ alert.lastNotifiedAt | date:'MMM d, y' }}</div>
                        <div class="triggered-time mat-body-small">{{ alert.lastNotifiedAt | date:'h:mm a' }}</div>
                      </div>
                      <ng-template #neverTriggered>
                        <div class="never-triggered mat-body-small">
                          <mat-icon>schedule</mat-icon>
                          Never triggered
                        </div>
                      </ng-template>
                    </td>
                  </ng-container>

                  <!-- Actions Column -->
                  <ng-container matColumnDef="actions">
                    <th mat-header-cell *matHeaderCellDef class="mat-title-medium actions-header">Actions</th>
                    <td mat-cell *matCellDef="let alert" class="actions-cell">
                      <div class="actions-container-modern">
                        <button mat-icon-button [matMenuTriggerFor]="actionsMenu">
                          <mat-icon>more_vert</mat-icon>
                        </button>
                        <mat-menu #actionsMenu="matMenu">
                          <button mat-menu-item (click)="editAlert(alert)">
                            <mat-icon>edit</mat-icon>
                            Edit Alert
                          </button>
                          <button mat-menu-item (click)="testAlert(alert)">
                            <mat-icon>play_arrow</mat-icon>
                            Test Alert
                          </button>
                          <button mat-menu-item (click)="duplicateAlert(alert)">
                            <mat-icon>content_copy</mat-icon>
                            Duplicate Alert
                          </button>
                          <mat-divider></mat-divider>
                          <button mat-menu-item (click)="deleteAlert(alert)" class="delete-action">
                            <mat-icon>delete</mat-icon>
                            Delete Alert
                          </button>
                        </mat-menu>
                      </div>
                    </td>
                  </ng-container>

                  <tr mat-header-row *matHeaderRowDef="displayedColumns" class="table-header"></tr>
                  <tr mat-row *matRowDef="let row; columns: displayedColumns;" class="table-row"></tr>
                </table>
              </div>
            </div>
          </mat-card-content>
        </mat-card>
      </section>
    </div>
  `,
  styleUrls: ['./alerts-list.component.scss']
})
export class AlertsListComponent implements OnInit {
  alerts: AlertRuleDto[] = [];
  displayedColumns: string[] = ['product', 'condition', 'status', 'lastTriggered', 'actions'];
  isLoading = false;
  isUpdating = false;

  constructor(
    private alertsService: AlertsService,
    private router: Router,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) { }

  ngOnInit(): void {
    this.loadAlerts();
  }

  loadAlerts(): void {
    this.isLoading = true;
    this.alertsService.getUserAlerts().subscribe({
      next: (alerts: AlertRuleDto[]) => {
        this.alerts = alerts;
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading alerts:', error);
        this.snackBar.open('Failed to load alerts', 'Close', { duration: 3000 });
        this.isLoading = false;
      }
    });
  }

  getActiveAlertsCount(): number {
    return this.alerts.filter(alert => alert.isActive).length;
  }

  getRecentTriggersCount(): number {
    const oneWeekAgo = new Date();
    oneWeekAgo.setDate(oneWeekAgo.getDate() - 7);
    return this.alerts.filter(alert => 
      alert.lastNotifiedAt && new Date(alert.lastNotifiedAt) >= oneWeekAgo
    ).length;
  }

  getUniqueConditionTypesCount(): number {
    const uniqueTypes = new Set(this.alerts.map(alert => alert.conditionType).filter(type => type));
    return uniqueTypes.size;
  }

  getTodayTriggersCount(): number {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return this.alerts.filter(alert => 
      alert.lastNotifiedAt && new Date(alert.lastNotifiedAt) >= today
    ).length;
  }

  getAlertsByType(): { oneShot: number; recurring: number } {
    return {
      oneShot: this.alerts.filter(a => a.alertType === 'ONE_SHOT').length,
      recurring: this.alerts.filter(a => a.alertType === 'RECURRING').length
    };
  }

  getConditionTypeBreakdown(): { [key: string]: number } {
    return this.alerts.reduce((breakdown, alert) => {
      const type = alert.conditionType || 'UNKNOWN';
      breakdown[type] = (breakdown[type] || 0) + 1;
      return breakdown;
    }, {} as { [key: string]: number });
  }

  getAverageNotificationFrequency(): number {
    if (this.alerts.length === 0) return 0;
    const totalFrequency = this.alerts.reduce((sum, alert) => 
      sum + (alert.notificationFrequencyMinutes || 0), 0);
    return Math.round(totalFrequency / this.alerts.length);
  }

  getMostRecentAlert(): AlertRuleDto | null {
    if (this.alerts.length === 0) return null;
    return this.alerts.reduce((newest, alert) => 
      new Date(alert.createdAt!) > new Date(newest.createdAt!) ? alert : newest);
  }

  hasNeverTriggeredAlerts(): boolean {
    return this.alerts.some(alert => !alert.lastNotifiedAt);
  }

  getProductsWithMostAlerts(): string {
    const productCounts = this.alerts.reduce((counts, alert) => {
      const productName = alert.product?.name || 'Unknown';
      counts[productName] = (counts[productName] || 0) + 1;
      return counts;
    }, {} as { [key: string]: number });

    const topProduct = Object.entries(productCounts).reduce((max, [product, count]) => 
      (count as number) > max.count ? { product, count: count as number } : max, { product: '', count: 0 });
    
    return topProduct.product || 'None';
  }

  createAlert(): void {
    this.router.navigate(['/alerts/create']);
  }

  viewPerformance(): void {
    this.router.navigate(['/alerts/performance']);
  }

  editAlert(alert: AlertRuleDto): void {
    this.router.navigate(['/alerts/edit', alert.alertRuleId]);
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
        alert.isActive = !isActive;
      }
    });
  }

  testAlert(alert: AlertRuleDto): void {
    this.snackBar.open('Alert testing functionality coming soon', 'Close', { duration: 2000 });
  }

  duplicateAlert(alert: AlertRuleDto): void {
    this.snackBar.open('Alert duplication functionality coming soon', 'Close', { duration: 2000 });
  }

  deleteAlert(alert: AlertRuleDto): void {
    const productName = alert.product?.name || 'this alert';
    if (confirm(`Are you sure you want to delete the alert for "${productName}"?\n\nThis action cannot be undone.`)) {
      this.alertsService.deleteAlert(alert.alertRuleId!).subscribe({
        next: () => {
          this.alerts = this.alerts.filter(a => a.alertRuleId !== alert.alertRuleId);
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
        return `Below $${alert.thresholdValue}`;
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

  getConditionChipClass(conditionType: string): string {
    switch (conditionType) {
      case 'PRICE_BELOW':
        return 'primary';
      case 'PERCENT_DROP_FROM_LAST':
        return 'secondary';
      case 'BACK_IN_STOCK':
        return 'tertiary';
      default:
        return 'primary';
    }
  }

  getConditionIcon(conditionType: string): string {
    switch (conditionType) {
      case 'PRICE_BELOW':
        return 'trending_down';
      case 'PERCENT_DROP_FROM_LAST':
        return 'percent';
      case 'BACK_IN_STOCK':
        return 'inventory';
      default:
        return 'help';
    }
  }

  getAlertTypeIcon(alertType: string): string {
    switch (alertType) {
      case 'RECURRING':
        return 'repeat';
      case 'ONE_SHOT':
        return 'play_arrow';
      default:
        return 'notification_important';
    }
  }

  getRelativeTime(date: string): string {
    const now = new Date();
    const past = new Date(date);
    const diffMs = now.getTime() - past.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays} days ago`;
    if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`;
    return `${Math.floor(diffDays / 30)} months ago`;
  }
}
