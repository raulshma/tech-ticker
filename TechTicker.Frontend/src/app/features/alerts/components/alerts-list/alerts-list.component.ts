import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
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
    MatToolbarModule,
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
              <button mat-button variant="filled" color="primary" (click)="createAlert()">
                <mat-icon>add</mat-icon>
                Create Alert
              </button>
              <button mat-button variant="filled" color="accent" (click)="viewPerformance()">
                <mat-icon>analytics</mat-icon>
                Performance
              </button>
              <button mat-button variant="outlined" (click)="loadAlerts()">
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
            <div class="empty-state" *ngIf="!isLoading && alerts.length === 0">
              <div class="empty-content">
                <div class="empty-icon-wrapper">
                  <mat-icon class="empty-icon">notifications_none</mat-icon>
                </div>
                <h3 class="mat-headline-medium empty-title">No Alert Rules Yet</h3>
                <p class="mat-body-large empty-description">
                  Create your first alert rule to get notified about price changes and product availability.
                </p>
                <div class="empty-actions">
                  <button mat-button variant="filled" color="primary" (click)="createAlert()">
                    <mat-icon>add</mat-icon>
                    Create Your First Alert
                  </button>
                  <button mat-button variant="outlined" routerLink="/catalog">
                    <mat-icon>search</mat-icon>
                    Browse Products
                  </button>
                </div>
                <div class="empty-features">
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
              <table mat-table [dataSource]="alerts" class="modern-table">
                <!-- Product Column -->
                <ng-container matColumnDef="product">
                  <th mat-header-cell *matHeaderCellDef class="mat-title-medium">Product</th>
                  <td mat-cell *matCellDef="let alert">
                    <div class="product-info">
                      <div class="product-avatar">
                        <mat-icon>inventory_2</mat-icon>
                      </div>
                      <div class="product-details">
                        <div class="product-name mat-body-medium">{{ alert.product?.name }}</div>
                        <div class="product-brand mat-body-small">{{ alert.product?.manufacturer }}</div>
                        <div class="seller-name mat-body-small" *ngIf="alert.specificSellerName">
                          Seller: {{ alert.specificSellerName }}
                        </div>
                      </div>
                    </div>
                  </td>
                </ng-container>

                <!-- Condition Column -->
                <ng-container matColumnDef="condition">
                  <th mat-header-cell *matHeaderCellDef class="mat-title-medium">Condition</th>
                  <td mat-cell *matCellDef="let alert">
                    <div class="condition-info">
                      <mat-chip [color]="getConditionColor(alert.conditionType)" class="condition-chip">
                        <mat-icon>{{ getConditionIcon(alert.conditionType) }}</mat-icon>
                        {{ getConditionText(alert) }}
                      </mat-chip>
                      <div class="condition-details" *ngIf="alert.ruleDescription">
                        <span class="description-text mat-body-small">{{ alert.ruleDescription }}</span>
                      </div>
                      <div class="alert-type-badge">
                        <span class="type-badge" [class]="alert.alertType === 'ONE_SHOT' ? 'one-shot-badge' : 'recurring-badge'">
                          {{ alert.alertType === 'ONE_SHOT' ? 'ONE-TIME' : 'RECURRING' }}
                        </span>
                      </div>
                    </div>
                  </td>
                </ng-container>

                <!-- Status Column -->
                <ng-container matColumnDef="status">
                  <th mat-header-cell *matHeaderCellDef class="mat-title-medium">Status</th>
                  <td mat-cell *matCellDef="let alert">
                    <div class="status-info">
                      <mat-slide-toggle
                        [checked]="alert.isActive"
                        (change)="toggleAlert(alert, $event.checked)"
                        [disabled]="isUpdating"
                        [color]="'primary'">
                      </mat-slide-toggle>
                      <div class="status-details">
                        <span class="status-text mat-body-medium">
                          {{ alert.isActive ? 'Active' : 'Inactive' }}
                        </span>
                        <span class="status-badge" [class]="alert.isActive ? 'active-badge' : 'inactive-badge'">
                          {{ alert.isActive ? 'ENABLED' : 'DISABLED' }}
                        </span>
                      </div>
                    </div>
                  </td>
                </ng-container>

                <!-- Last Triggered Column -->
                <ng-container matColumnDef="lastTriggered">
                  <th mat-header-cell *matHeaderCellDef class="mat-title-medium">Last Triggered</th>
                  <td mat-cell *matCellDef="let alert">
                    <div class="last-triggered">
                      <div class="triggered-date mat-body-medium" *ngIf="alert.lastNotifiedAt; else neverTriggered">
                        {{ alert.lastNotifiedAt | date:'short' }}
                      </div>
                      <ng-template #neverTriggered>
                        <div class="never-triggered mat-body-medium">Never</div>
                      </ng-template>
                      <div class="triggered-relative mat-body-small" *ngIf="alert.lastNotifiedAt">
                        {{ getRelativeTime(alert.lastNotifiedAt) }}
                      </div>
                    </div>
                  </td>
                </ng-container>

                <!-- Actions Column -->
                <ng-container matColumnDef="actions">
                  <th mat-header-cell *matHeaderCellDef class="mat-title-medium">Actions</th>
                  <td mat-cell *matCellDef="let alert">
                    <button mat-icon-button [matMenuTriggerFor]="actionsMenu" class="actions-button">
                      <mat-icon>more_vert</mat-icon>
                    </button>
                    <mat-menu #actionsMenu="matMenu" class="actions-menu">
                      <button mat-menu-item (click)="editAlert(alert)">
                        <mat-icon>edit</mat-icon>
                        <span>Edit Alert</span>
                      </button>
                      <button mat-menu-item (click)="testAlert(alert)">
                        <mat-icon>play_arrow</mat-icon>
                        <span>Test Alert</span>
                      </button>
                      <button mat-menu-item (click)="duplicateAlert(alert)">
                        <mat-icon>content_copy</mat-icon>
                        <span>Duplicate Alert</span>
                      </button>
                      <mat-divider></mat-divider>
                      <button mat-menu-item (click)="deleteAlert(alert)" class="delete-action">
                        <mat-icon>delete</mat-icon>
                        <span>Delete Alert</span>
                      </button>
                    </mat-menu>
                  </td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
                <tr mat-row *matRowDef="let row; columns: displayedColumns;" class="table-row"></tr>
              </table>
            </div>
          </mat-card-content>
        </mat-card>
      </section>
    </div>
  `,
  styles: [`
    // Modern Material Design 3 Alerts List Styles
    .alerts-list-container {
      min-height: 100vh;
      background: var(--mat-sys-surface);
      padding: 0;
    }

    // ===== WELCOME SECTION =====
    .welcome-section {
      background: linear-gradient(135deg, var(--mat-sys-primary-container) 0%, var(--mat-sys-secondary-container) 100%);
      padding: 24px;
      margin-bottom: 24px;
      border-radius: 0;

      .welcome-content {
        max-width: 1400px;
        margin: 0 auto;
      }

      .header-main {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 24px;

        @media (max-width: 768px) {
          flex-direction: column;
          align-items: stretch;
          gap: 16px;
        }
      }

      .title-section {
        h1 {
          color: var(--mat-sys-on-primary-container);
          margin: 0 0 8px 0;
          font-weight: 400;
          line-height: 1.2;
        }

        .welcome-subtitle {
          color: var(--mat-sys-on-primary-container);
          margin: 0;
          opacity: 0.87;
        }
      }

      .header-actions {
        display: flex;
        gap: 12px;
        flex-wrap: wrap;

        @media (max-width: 768px) {
          width: 100%;
          justify-content: stretch;
          
          button {
            flex: 1;
            min-width: 120px;
          }
        }
      }
    }

    // ===== STATISTICS SECTION =====
    .stats-section {
      margin: 0 24px 24px;

      .stats-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
        gap: 16px;
        max-width: 1400px;
        margin: 0 auto;

        @media (max-width: 768px) {
          grid-template-columns: 1fr;
          gap: 12px;
        }
      }

      .stat-card {
        border-radius: 16px;
        background: var(--mat-sys-surface-container);
        transition: all 0.3s ease;
        cursor: pointer;

        &:hover {
          transform: translateY(-2px);
          box-shadow: var(--mat-sys-elevation-level2);
        }

        &:focus {
          outline: 2px solid var(--mat-sys-primary);
          outline-offset: 2px;
        }

        mat-card-content {
          padding: 20px !important;
        }

        .stat-content {
          display: flex;
          align-items: center;
          gap: 16px;
        }

        .stat-icon {
          width: 48px;
          height: 48px;
          border-radius: 12px;
          display: flex;
          align-items: center;
          justify-content: center;
          flex-shrink: 0;

          mat-icon {
            font-size: 24px;
            width: 24px;
            height: 24px;
          }

          &.primary-surface {
            background: var(--mat-sys-primary-container);
            color: var(--mat-sys-on-primary-container);
          }

          &.success-surface {
            background: var(--mat-sys-tertiary-container);
            color: var(--mat-sys-on-tertiary-container);
          }

          &.secondary-surface {
            background: var(--mat-sys-secondary-container);
            color: var(--mat-sys-on-secondary-container);
          }

          &.info-surface {
            background: var(--mat-sys-surface-variant);
            color: var(--mat-sys-on-surface-variant);
          }
        }

        .stat-info {
          flex: 1;
          min-width: 0;

          .stat-number {
            margin: 0 0 4px 0;
            color: var(--mat-sys-on-surface);
            font-weight: 600;
            line-height: 1;
          }

          .stat-label {
            margin: 0;
            color: var(--mat-sys-on-surface-variant);
            text-transform: uppercase;
            letter-spacing: 0.5px;
            font-weight: 500;
          }
        }
      }
    }

    // ===== MANAGEMENT SECTION =====
    .management-section {
      margin: 0 24px;
      max-width: 1400px;
      margin-left: auto;
      margin-right: auto;
      padding: 0 24px;

      .management-card {
        border-radius: 16px;
        background: var(--mat-sys-surface-container);

        .management-header {
          padding: 24px 24px 16px;
          border-bottom: 1px solid var(--mat-sys-outline-variant);

          mat-card-title {
            color: var(--mat-sys-on-surface);
            margin-bottom: 8px;
          }

          mat-card-subtitle {
            color: var(--mat-sys-on-surface-variant);
            margin: 0;
          }
        }

        .management-content {
          padding: 24px !important;
        }
      }
    }

    // ===== LOADING SECTION =====
    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px;
      min-height: 300px;

      .loading-text {
        margin-top: 16px;
        color: var(--mat-sys-on-surface-variant);
      }
    }

    // ===== EMPTY STATE =====
    .empty-state {
      text-align: center;
      padding: 48px 24px;

      .empty-content {
        max-width: 500px;
        margin: 0 auto;
      }

      .empty-icon-wrapper {
        margin-bottom: 24px;

        .empty-icon {
          font-size: 72px;
          width: 72px;
          height: 72px;
          color: var(--mat-sys-outline);
          opacity: 0.6;
        }
      }

      .empty-title {
        margin: 0 0 16px 0;
        color: var(--mat-sys-on-surface);
      }

      .empty-description {
        margin: 0 0 32px 0;
        color: var(--mat-sys-on-surface-variant);
        line-height: 1.5;
      }

      .empty-actions {
        display: flex;
        gap: 12px;
        justify-content: center;
        flex-wrap: wrap;
        margin-bottom: 32px;

        @media (max-width: 480px) {
          flex-direction: column;
          
          button {
            width: 100%;
          }
        }
      }

      .empty-features {
        display: flex;
        justify-content: center;
        gap: 24px;
        flex-wrap: wrap;
        opacity: 0.7;

        @media (max-width: 480px) {
          flex-direction: column;
          gap: 12px;
        }

        .feature-item {
          display: flex;
          align-items: center;
          gap: 8px;
          color: var(--mat-sys-on-surface-variant);

          mat-icon {
            font-size: 20px;
            width: 20px;
            height: 20px;
          }

          span {
            font-size: 14px;
          }
        }
      }
    }

    // ===== TABLE SECTION =====
    .table-container {
      .modern-table {
        width: 100%;
        background: var(--mat-sys-surface);
        border-radius: 12px;
        overflow: hidden;
        box-shadow: var(--mat-sys-elevation-level1);

        th {
          background: var(--mat-sys-surface-variant);
          color: var(--mat-sys-on-surface-variant);
          font-weight: 600;
          padding: 16px;
        }

        td {
          padding: 16px;
          border-bottom: 1px solid var(--mat-sys-outline-variant);
        }

        .table-row {
          transition: background-color 0.2s ease;

          &:hover {
            background: var(--mat-sys-surface-container);
          }
        }
      }

      .product-info {
        display: flex;
        align-items: center;
        gap: 12px;

        .product-avatar {
          width: 40px;
          height: 40px;
          border-radius: 10px;
          background: var(--mat-sys-tertiary-container);
          display: flex;
          align-items: center;
          justify-content: center;

          mat-icon {
            color: var(--mat-sys-on-tertiary-container);
            font-size: 20px;
          }
        }

        .product-details {
          .product-name {
            font-weight: 500;
            color: var(--mat-sys-on-surface);
            margin-bottom: 2px;
          }

          .product-brand, .seller-name {
            color: var(--mat-sys-on-surface-variant);
          }
        }
      }

      .condition-info {
        .condition-chip {
          margin-bottom: 8px;
          
          mat-icon {
            font-size: 16px;
            margin-right: 4px;
          }
        }

        .condition-details {
          margin-bottom: 4px;

          .description-text {
            color: var(--mat-sys-on-surface-variant);
            font-style: italic;
          }
        }

        .alert-type-badge {
          .type-badge {
            font-size: 10px;
            padding: 2px 6px;
            border-radius: 4px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.5px;

            &.one-shot-badge {
              background: var(--mat-sys-error-container);
              color: var(--mat-sys-on-error-container);
            }

            &.recurring-badge {
              background: var(--mat-sys-primary-container);
              color: var(--mat-sys-on-primary-container);
            }
          }
        }
      }

      .status-info {
        display: flex;
        align-items: center;
        gap: 12px;

        .status-details {
          .status-text {
            display: block;
            margin-bottom: 2px;
          }

          .status-badge {
            font-size: 10px;
            padding: 2px 6px;
            border-radius: 4px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.5px;

            &.active-badge {
              background: var(--mat-sys-tertiary-container);
              color: var(--mat-sys-on-tertiary-container);
            }

            &.inactive-badge {
              background: var(--mat-sys-error-container);
              color: var(--mat-sys-on-error-container);
            }
          }
        }
      }

      .last-triggered {
        .triggered-date, .never-triggered {
          margin-bottom: 2px;
        }

        .triggered-relative {
          color: var(--mat-sys-on-surface-variant);
        }

        .never-triggered {
          color: var(--mat-sys-on-surface-variant);
          font-style: italic;
        }
      }

      .actions-button {
        color: var(--mat-sys-on-surface-variant);
        
        &:hover {
          background: var(--mat-sys-surface-container-highest);
        }
      }

      .actions-menu {
        .delete-action {
          color: var(--mat-sys-error);
          
          mat-icon {
            color: var(--mat-sys-error);
          }
        }
      }
    }

    // ===== RESPONSIVE DESIGN =====
    @media (max-width: 768px) {
      .alerts-list-container {
        padding: 0;
      }

      .management-section {
        margin: 0 16px;
        padding: 0 16px;
      }

      .stats-section {
        margin: 0 16px 16px;
      }

      .modern-table {
        display: block;
        overflow-x: auto;
        white-space: nowrap;
      }
    }
  `]
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
      next: (alerts) => {
        this.alerts = alerts;
        this.isLoading = false;
      },
      error: (error) => {
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
    const last7Days = new Date();
    last7Days.setDate(last7Days.getDate() - 7);
    return this.alerts.filter(alert => 
      alert.lastNotifiedAt && new Date(alert.lastNotifiedAt) >= last7Days
    ).length;
  }

  getUniqueConditionTypesCount(): number {
    const uniqueTypes = new Set(this.alerts.map(alert => alert.conditionType));
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
    const frequencies = this.alerts
      .map(alert => alert.notificationFrequencyMinutes || 0)
      .filter(freq => freq > 0);
    
    return frequencies.length > 0 
      ? frequencies.reduce((sum, freq) => sum + freq, 0) / frequencies.length 
      : 0;
  }

  getMostRecentAlert(): AlertRuleDto | null {
    return this.alerts
      .filter(alert => alert.lastNotifiedAt)
      .sort((a, b) => new Date(b.lastNotifiedAt!).getTime() - new Date(a.lastNotifiedAt!).getTime())[0] || null;
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

    const topProduct = Object.entries(productCounts)
      .sort(([,a], [,b]) => b - a)[0];
    
    return topProduct ? `${topProduct[0]} (${topProduct[1]} alerts)` : 'None';
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
    this.snackBar.open('Alert testing feature coming soon', 'Close', { duration: 2000 });
  }

  duplicateAlert(alert: AlertRuleDto): void {
    this.snackBar.open('Alert duplication feature coming soon', 'Close', { duration: 2000 });
  }

  deleteAlert(alert: AlertRuleDto): void {
    if (confirm(`Are you sure you want to delete the alert for "${alert.product?.name}"?`)) {
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
