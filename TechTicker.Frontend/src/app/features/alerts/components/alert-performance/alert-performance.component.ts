import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { TechTickerApiClient } from '../../../../shared/api/api-client';

@Component({
  selector: 'app-alert-performance',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatTableModule,
    MatChipsModule,
    MatProgressBarModule,
    MatSnackBarModule
  ],
  template: `
    <div class="performance-container">
      <mat-card class="header-card">
        <mat-card-header>
          <mat-card-title>Alert System Performance</mat-card-title>
          <mat-card-subtitle>Monitor alert evaluation and notification delivery</mat-card-subtitle>
        </mat-card-header>
        <mat-card-actions>
          <button mat-raised-button color="primary" (click)="refreshData()">
            <mat-icon>refresh</mat-icon>
            Refresh
          </button>
        </mat-card-actions>
      </mat-card>

      <mat-tab-group class="performance-tabs">
        <!-- System Overview Tab -->
        <mat-tab label="System Overview">
          <div class="tab-content">
            <!-- System Health Cards -->
            <div class="metrics-grid">
              <mat-card class="metric-card">
                <mat-card-content>
                  <div class="metric-header">
                    <mat-icon [class]="systemHealth?.isHealthy ? 'healthy' : 'unhealthy'">
                      {{ systemHealth?.isHealthy ? 'check_circle' : 'error' }}
                    </mat-icon>
                    <h3>System Health</h3>
                  </div>
                  <div class="metric-value">
                    {{ systemHealth?.isHealthy ? 'Healthy' : 'Issues Detected' }}
                  </div>
                  <div class="metric-details" *ngIf="systemHealth?.healthIssues?.length">
                    <mat-chip-listbox>
                      <mat-chip *ngFor="let issue of systemHealth.healthIssues">
                        {{ issue }}
                      </mat-chip>
                    </mat-chip-listbox>
                  </div>
                </mat-card-content>
              </mat-card>

              <mat-card class="metric-card">
                <mat-card-content>
                  <div class="metric-header">
                    <mat-icon>rule</mat-icon>
                    <h3>Active Rules</h3>
                  </div>
                  <div class="metric-value">
                    {{ systemHealth?.activeAlertRules || 0 }}
                  </div>
                  <div class="metric-subtitle">
                    {{ systemHealth?.inactiveAlertRules || 0 }} inactive
                  </div>
                </mat-card-content>
              </mat-card>

              <mat-card class="metric-card">
                <mat-card-content>
                  <div class="metric-header">
                    <mat-icon>notifications</mat-icon>
                    <h3>Notifications Today</h3>
                  </div>
                  <div class="metric-value">
                    {{ realTimeData?.notificationsInLastHour || 0 }}
                  </div>
                  <div class="metric-subtitle">
                    {{ realTimeData?.alertsInLastHour || 0 }} alerts triggered
                  </div>
                </mat-card-content>
              </mat-card>

              <mat-card class="metric-card">
                <mat-card-content>
                  <div class="metric-header">
                    <mat-icon>speed</mat-icon>
                    <h3>Avg Response Time</h3>
                  </div>
                  <div class="metric-value">
                    {{ formatDuration(realTimeData?.averageProcessingTime) }}
                  </div>
                  <div class="metric-subtitle">
                    Processing time
                  </div>
                </mat-card-content>
              </mat-card>
            </div>

            <!-- Recent Activity -->
            <mat-card class="activity-card">
              <mat-card-header>
                <mat-card-title>Recent Alert Activity</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <div class="activity-list" *ngIf="realTimeData?.recentActivity?.length; else noActivity">
                  <div class="activity-item" *ngFor="let activity of realTimeData.recentActivity">
                    <div class="activity-icon">
                      <mat-icon [class]="getActivityIconClass(activity.status)">
                        {{ getActivityIcon(activity.status) }}
                      </mat-icon>
                    </div>
                    <div class="activity-content">
                      <div class="activity-title">{{ activity.productName }}</div>
                      <div class="activity-details">
                        {{ activity.conditionType }} - {{ formatPrice(activity.triggeringPrice) }}
                      </div>
                      <div class="activity-time">
                        {{ activity.timestamp | date:'short' }}
                      </div>
                    </div>
                    <div class="activity-status">
                      <mat-chip [class]="getStatusChipClass(activity.status)">
                        {{ activity.status }}
                      </mat-chip>
                    </div>
                  </div>
                </div>
                <ng-template #noActivity>
                  <div class="no-activity">
                    <mat-icon>inbox</mat-icon>
                    <p>No recent alert activity</p>
                  </div>
                </ng-template>
              </mat-card-content>
            </mat-card>
          </div>
        </mat-tab>

        <!-- Performance Metrics Tab -->
        <mat-tab label="Performance Metrics">
          <div class="tab-content">
            <mat-card class="metrics-card">
              <mat-card-header>
                <mat-card-title>Evaluation Metrics</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <div class="metrics-row">
                  <div class="metric-item">
                    <span class="metric-label">Total Evaluations:</span>
                    <span class="metric-value">{{ systemPerformance?.evaluationMetrics?.totalAlertsEvaluated || 0 }}</span>
                  </div>
                  <div class="metric-item">
                    <span class="metric-label">Trigger Rate:</span>
                    <span class="metric-value">{{ formatPercentage(systemPerformance?.evaluationMetrics?.alertTriggerRate) }}</span>
                  </div>
                  <div class="metric-item">
                    <span class="metric-label">Error Rate:</span>
                    <span class="metric-value">{{ formatPercentage(systemPerformance?.evaluationMetrics?.errorRate) }}</span>
                  </div>
                  <div class="metric-item">
                    <span class="metric-label">Avg Evaluation Time:</span>
                    <span class="metric-value">{{ formatDuration(systemPerformance?.evaluationMetrics?.averageEvaluationTime) }}</span>
                  </div>
                </div>
              </mat-card-content>
            </mat-card>

            <mat-card class="metrics-card">
              <mat-card-header>
                <mat-card-title>Notification Metrics</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <div class="metrics-row">
                  <div class="metric-item">
                    <span class="metric-label">Total Sent:</span>
                    <span class="metric-value">{{ systemPerformance?.notificationMetrics?.totalNotificationsSent || 0 }}</span>
                  </div>
                  <div class="metric-item">
                    <span class="metric-label">Success Rate:</span>
                    <span class="metric-value">{{ formatPercentage(systemPerformance?.notificationMetrics?.deliverySuccessRate) }}</span>
                  </div>
                  <div class="metric-item">
                    <span class="metric-label">Failed:</span>
                    <span class="metric-value">{{ systemPerformance?.notificationMetrics?.failedDeliveries || 0 }}</span>
                  </div>
                  <div class="metric-item">
                    <span class="metric-label">Avg Delivery Time:</span>
                    <span class="metric-value">{{ formatDuration(systemPerformance?.notificationMetrics?.averageDeliveryTime) }}</span>
                  </div>
                </div>
              </mat-card-content>
            </mat-card>
          </div>
        </mat-tab>

        <!-- System Events Tab -->
        <mat-tab label="System Events">
          <div class="tab-content">
            <mat-card class="events-card">
              <mat-card-header>
                <mat-card-title>Recent System Events</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <div class="events-list" *ngIf="realTimeData?.recentEvents?.length; else noEvents">
                  <div class="event-item" *ngFor="let event of realTimeData.recentEvents">
                    <div class="event-icon">
                      <mat-icon [class]="getEventIconClass(event.eventType)">
                        {{ getEventIcon(event.eventType) }}
                      </mat-icon>
                    </div>
                    <div class="event-content">
                      <div class="event-message">{{ event.message }}</div>
                      <div class="event-details">
                        <span class="event-component" *ngIf="event.component">{{ event.component }}</span>
                        <span class="event-time">{{ event.timestamp | date:'short' }}</span>
                      </div>
                    </div>
                    <div class="event-type">
                      <mat-chip [class]="getEventTypeChipClass(event.eventType)">
                        {{ event.eventType }}
                      </mat-chip>
                    </div>
                  </div>
                </div>
                <ng-template #noEvents>
                  <div class="no-events">
                    <mat-icon>event_note</mat-icon>
                    <p>No recent system events</p>
                  </div>
                </ng-template>
              </mat-card-content>
            </mat-card>
          </div>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
  styles: [`
    .performance-container {
      padding: 20px;
      max-width: 1200px;
      margin: 0 auto;
    }

    .header-card {
      margin-bottom: 20px;
    }

    .performance-tabs {
      margin-top: 20px;
    }

    .tab-content {
      padding: 20px 0;
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 16px;
      margin-bottom: 24px;
    }

    .metric-card {
      text-align: center;
    }

    .metric-header {
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 16px;
    }

    .metric-header mat-icon {
      margin-right: 8px;
      font-size: 24px;
    }

    .metric-header h3 {
      margin: 0;
      font-size: 16px;
      color: #666;
    }

    .metric-value {
      font-size: 32px;
      font-weight: bold;
      margin-bottom: 8px;
    }

    .metric-subtitle {
      font-size: 14px;
      color: #999;
    }

    .healthy {
      color: #4caf50;
    }

    .unhealthy {
      color: #f44336;
    }

    .activity-card, .metrics-card, .events-card {
      margin-bottom: 20px;
    }

    .activity-list, .events-list {
      max-height: 400px;
      overflow-y: auto;
    }

    .activity-item, .event-item {
      display: flex;
      align-items: center;
      padding: 12px 0;
      border-bottom: 1px solid #eee;
    }

    .activity-icon, .event-icon {
      margin-right: 16px;
    }

    .activity-content, .event-content {
      flex: 1;
    }

    .activity-title, .event-message {
      font-weight: 500;
      margin-bottom: 4px;
    }

    .activity-details, .event-details {
      font-size: 12px;
      color: #666;
    }

    .activity-time, .event-time {
      font-size: 11px;
      color: #999;
    }

    .metrics-row {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
    }

    .metric-item {
      display: flex;
      justify-content: space-between;
      padding: 8px 0;
    }

    .metric-label {
      color: #666;
    }

    .metric-value {
      font-weight: 500;
    }

    .no-activity, .no-events {
      text-align: center;
      padding: 40px;
      color: #999;
    }

    .no-activity mat-icon, .no-events mat-icon {
      font-size: 48px;
      margin-bottom: 16px;
    }

    .status-triggered { color: #ff9800; }
    .status-notified { color: #4caf50; }
    .status-failed { color: #f44336; }

    .event-error { color: #f44336; }
    .event-warning { color: #ff9800; }
    .event-info { color: #2196f3; }
  `]
})
export class AlertPerformanceComponent implements OnInit {
  systemPerformance: any;
  systemHealth: any;
  realTimeData: any;
  isLoading = false;

  constructor(
    private apiClient: TechTickerApiClient,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadPerformanceData();
  }

  loadPerformanceData(): void {
    this.isLoading = true;

    // Load system performance
    this.apiClient.getSystemPerformance(undefined, undefined).subscribe({
      next: (response) => {
        this.systemPerformance = response.data;
      },
      error: (error) => {
        console.error('Error loading system performance:', error);
      }
    });

    // Load system health
    this.apiClient.getSystemHealth().subscribe({
      next: (response) => {
        this.systemHealth = response.data;
      },
      error: (error) => {
        console.error('Error loading system health:', error);
      }
    });

    // Load real-time data
    this.apiClient.getRealTimeMonitoring().subscribe({
      next: (response) => {
        this.realTimeData = response.data;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading real-time data:', error);
        this.isLoading = false;
      }
    });
  }

  refreshData(): void {
    this.loadPerformanceData();
    this.snackBar.open('Performance data refreshed', 'Close', { duration: 2000 });
  }

  formatDuration(duration: any): string {
    if (!duration) return 'N/A';
    // Assuming duration is in milliseconds or a timespan string
    if (typeof duration === 'string') {
      return duration;
    }
    return `${duration}ms`;
  }

  formatPercentage(value: number | undefined): string {
    if (value === undefined || value === null) return 'N/A';
    return `${value.toFixed(1)}%`;
  }

  getActivityIcon(status: string): string {
    switch (status) {
      case 'TRIGGERED': return 'notification_important';
      case 'NOTIFIED': return 'check_circle';
      case 'FAILED': return 'error';
      default: return 'info';
    }
  }

  getActivityIconClass(status: string): string {
    switch (status) {
      case 'TRIGGERED': return 'status-triggered';
      case 'NOTIFIED': return 'status-notified';
      case 'FAILED': return 'status-failed';
      default: return '';
    }
  }

  getStatusChipClass(status: string): string {
    return `status-${status.toLowerCase()}`;
  }

  getEventIcon(eventType: string): string {
    switch (eventType) {
      case 'ERROR': return 'error';
      case 'WARNING': return 'warning';
      case 'INFO': return 'info';
      default: return 'event';
    }
  }

  getEventIconClass(eventType: string): string {
    switch (eventType) {
      case 'ERROR': return 'event-error';
      case 'WARNING': return 'event-warning';
      case 'INFO': return 'event-info';
      default: return '';
    }
  }

  getEventTypeChipClass(eventType: string): string {
    return `event-${eventType.toLowerCase()}`;
  }

  formatPrice(price: number): string {
    return `$${price?.toFixed(2) || '0.00'}`;
  }
}
