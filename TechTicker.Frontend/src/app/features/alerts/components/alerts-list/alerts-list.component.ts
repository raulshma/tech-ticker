import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
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

import { AlertsService } from '../../services/alerts.service';
import { AlertRuleDto, UpdateAlertRuleDto } from '../../../../shared/api/api-client';

@Component({
  selector: 'app-alerts-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatToolbarModule,
    MatTableModule,
    MatChipsModule,
    MatSlideToggleModule,
    MatMenuModule,
    MatSnackBarModule,
    MatDialogModule
  ],
  template: `
    <div class="alerts-container">
      <mat-toolbar>
        <span>Alert Rules</span>
        <span class="spacer"></span>
        <button mat-button color="accent" (click)="viewPerformance()">
          <mat-icon>analytics</mat-icon>
          Performance
        </button>
        <button mat-raised-button color="primary" (click)="createAlert()">
          <mat-icon>add</mat-icon>
          Create Alert
        </button>
      </mat-toolbar>

      <mat-card class="alerts-card" *ngIf="!isLoading">
        <mat-card-header>
          <mat-card-title>Your Alert Rules ({{ alerts.length }})</mat-card-title>
          <mat-card-subtitle>Manage your price and availability alerts</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <!-- Empty State -->
          <div class="empty-state" *ngIf="alerts.length === 0">
            <mat-icon class="empty-icon">notifications_none</mat-icon>
            <h3>No Alert Rules Yet</h3>
            <p>Create your first alert rule to get notified about price changes and product availability.</p>
            <button mat-raised-button color="primary" (click)="createAlert()">
              <mat-icon>add</mat-icon>
              Create Your First Alert
            </button>
          </div>

          <!-- Alerts Table -->
          <div class="alerts-table-container" *ngIf="alerts.length > 0">
            <table mat-table [dataSource]="alerts" class="alerts-table">
              <!-- Product Column -->
              <ng-container matColumnDef="product">
                <th mat-header-cell *matHeaderCellDef>Product</th>
                <td mat-cell *matCellDef="let alert">
                  <div class="product-info">
                    <div class="product-name">{{ alert.product?.name }}</div>
                    <div class="product-brand">{{ alert.product?.manufacturer }}</div>
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
                    <div class="condition-details" *ngIf="alert.ruleDescription">
                      {{ alert.ruleDescription }}
                    </div>
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
                    <div class="alert-type">
                      {{ alert.alertType === 'ONE_SHOT' ? 'One-time' : 'Recurring' }}
                    </div>
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
                    <button mat-menu-item (click)="editAlert(alert)">
                      <mat-icon>edit</mat-icon>
                      <span>Edit</span>
                    </button>
                    <button mat-menu-item (click)="testAlert(alert)">
                      <mat-icon>play_arrow</mat-icon>
                      <span>Test</span>
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
          </div>
        </mat-card-content>
      </mat-card>

      <!-- Loading State -->
      <mat-card class="loading-card" *ngIf="isLoading">
        <mat-card-content>
          <div class="loading-state">
            <mat-icon class="loading-icon">hourglass_empty</mat-icon>
            <p>Loading your alert rules...</p>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .alerts-container {
      padding: 20px;
      max-width: 1200px;
      margin: 0 auto;
    }

    .alerts-card {
      margin-top: 20px;
    }

    .spacer {
      flex: 1 1 auto;
    }

    .empty-state {
      text-align: center;
      padding: 60px 20px;
    }

    .empty-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #ccc;
      margin-bottom: 16px;
    }

    .empty-state h3 {
      margin: 16px 0 8px 0;
      color: #666;
    }

    .empty-state p {
      color: #999;
      margin-bottom: 24px;
      max-width: 400px;
      margin-left: auto;
      margin-right: auto;
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
        // Revert the toggle
        alert.isActive = !isActive;
      }
    });
  }

  testAlert(alert: AlertRuleDto): void {
    // TODO: Implement alert testing
    this.snackBar.open('Alert testing feature coming soon', 'Close', { duration: 2000 });
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
