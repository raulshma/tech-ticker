import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonModule } from '@angular/material/button';

import { 
  AlertTestResultDto, 
  AlertRuleDto,
  AlertTestMatchDto 
} from '../../../../shared/api/api-client';

@Component({
  selector: 'app-alert-test-results',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatChipsModule,
    MatTableModule,
    MatExpansionModule,
    MatDividerModule,
    MatTooltipModule,
    MatButtonModule
  ],
  template: `
    <div class="test-results-container">
      <!-- Summary Card -->
      <mat-card class="summary-card" appearance="outlined">
        <mat-card-header>
          <div class="result-icon" [class]="getResultIconClass()">
            <mat-icon>{{ getResultIcon() }}</mat-icon>
          </div>
          <mat-card-title>{{ getResultTitle() }}</mat-card-title>
          <mat-card-subtitle>{{ getResultSubtitle() }}</mat-card-subtitle>
        </mat-card-header>
        
        <mat-card-content>
          <div class="summary-stats">
            <div class="stat-item">
              <span class="stat-label">Test Type</span>
              <mat-chip class="stat-chip">{{ getTestTypeLabel() }}</mat-chip>
            </div>
            
            <div class="stat-item">
              <span class="stat-label">Points Tested</span>
              <span class="stat-value">{{ results.totalPointsTested || 0 }}</span>
            </div>
            
            <div class="stat-item" *ngIf="results.testType === 'HISTORICAL_RANGE'">
              <span class="stat-label">Triggers Found</span>
              <span class="stat-value trigger-count" [class]="getTriggerCountClass()">
                {{ results.triggeredCount || 0 }}
              </span>
            </div>
            
            <div class="stat-item" *ngIf="results.testType === 'HISTORICAL_RANGE' && (results.totalPointsTested || 0) > 0">
              <span class="stat-label">Trigger Rate</span>
              <span class="stat-value">{{ getTriggerRate() }}%</span>
            </div>

            <!-- Notification Stats -->
            <div class="stat-item" *ngIf="results.notificationsEnabled">
              <span class="stat-label">Notifications</span>
              <mat-chip class="notification-chip" [class]="getSummaryNotificationChipClass()">
                <mat-icon>{{ getSummaryNotificationIcon() }}</mat-icon>
                {{ getSummaryNotificationText() }}
              </mat-chip>
            </div>

            <div class="stat-item" *ngIf="results.notificationsEnabled && results.overallNotificationStatus">
              <span class="stat-label">Status</span>
              <span class="stat-value notification-status" [class]="getNotificationStatusClass()">
                {{ results.overallNotificationStatus }}
              </span>
            </div>
          </div>

          <!-- Single Point Result -->
          <div class="single-result" *ngIf="results.testType === 'SINGLE_POINT' && results.matches && results.matches.length > 0">
            <mat-divider></mat-divider>
            <div class="result-explanation">
              <h4>Test Result Explanation</h4>
              <p class="explanation-text">
                <mat-icon class="explanation-icon" [color]="results.wouldTrigger ? 'primary' : 'warn'">
                  {{ results.wouldTrigger ? 'check_circle' : 'cancel' }}
                </mat-icon>
                {{ (results.matches && results.matches[0] && results.matches[0].triggerReason) || 'No trigger reason available' }}
              </p>
              
              <div class="test-details">
                <div class="detail-row">
                  <span class="detail-label">Test Price:</span>
                  <span class="detail-value price-value">\${{ (results.matches && results.matches[0] && results.matches[0].price) | number:'1.2-2' }}</span>
                </div>
                <div class="detail-row">
                  <span class="detail-label">Stock Status:</span>
                  <mat-chip class="stock-chip" [class]="getStockStatusClass((results.matches && results.matches[0] && results.matches[0].stockStatus) || '')">
                    {{ getStockStatusLabel((results.matches && results.matches[0] && results.matches[0].stockStatus) || '') }}
                  </mat-chip>
                </div>
                <div class="detail-row" *ngIf="results.matches && results.matches[0] && results.matches[0].sellerName">
                  <span class="detail-label">Seller:</span>
                  <span class="detail-value">{{ results.matches[0].sellerName }}</span>
                </div>

                <!-- Discord Notification Status for Single Point -->
                <div class="detail-row" *ngIf="results.notificationsEnabled">
                  <span class="detail-label">Discord Notification:</span>
                  <mat-chip class="notification-status-chip" [class]="getMatchNotificationChipClass(results.matches[0])">
                    <mat-icon>{{ getMatchNotificationIcon(results.matches[0]) }}</mat-icon>
                    {{ getMatchNotificationStatus(results.matches[0]) }}
                  </mat-chip>
                </div>

                <div class="detail-row" *ngIf="results.notificationsEnabled && results.matches && results.matches[0] && results.matches[0].notificationError">
                  <span class="detail-label">Notification Error:</span>
                  <span class="detail-value error-text">{{ results.matches[0].notificationError }}</span>
                </div>
              </div>
            </div>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- Historical Results Table -->
      <mat-card class="matches-card" appearance="outlined" *ngIf="results.testType === 'HISTORICAL_RANGE' && results.matches && results.matches.length > 0">
        <mat-card-header>
          <mat-card-title>
            <mat-icon>history</mat-icon>
            Historical Matches
          </mat-card-title>
          <mat-card-subtitle>
            Showing {{ getDisplayedMatches().length }} of {{ (results.matches && results.matches.length) || 0 }} price points
          </mat-card-subtitle>
        </mat-card-header>
        
        <mat-card-content>
          <div class="table-controls">
            <div class="view-toggle">
              <button mat-button 
                      [class.active]="showOnlyTriggers" 
                      (click)="toggleTriggerView()">
                <mat-icon>filter_list</mat-icon>
                {{ showOnlyTriggers ? 'Show All' : 'Triggers Only' }}
              </button>
            </div>
          </div>

          <div class="table-container">
            <table mat-table [dataSource]="getDisplayedMatches()" class="matches-table">
              <!-- Timestamp Column -->
              <ng-container matColumnDef="timestamp">
                <th mat-header-cell *matHeaderCellDef>Date</th>
                <td mat-cell *matCellDef="let match">
                  <div class="timestamp-cell">
                    <div class="date">{{ match.timestamp | date:'MMM d, y' }}</div>
                    <div class="time">{{ match.timestamp | date:'h:mm a' }}</div>
                  </div>
                </td>
              </ng-container>

              <!-- Price Column -->
              <ng-container matColumnDef="price">
                <th mat-header-cell *matHeaderCellDef>Price</th>
                <td mat-cell *matCellDef="let match">
                  <span class="price-cell" [class]="match.wouldTrigger ? 'trigger-price' : ''">
                    \${{ match.price | number:'1.2-2' }}
                  </span>
                </td>
              </ng-container>

              <!-- Stock Status Column -->
              <ng-container matColumnDef="stockStatus">
                <th mat-header-cell *matHeaderCellDef>Stock</th>
                <td mat-cell *matCellDef="let match">
                  <mat-chip class="stock-chip-small" [class]="getStockStatusClass(match.stockStatus)">
                    {{ getStockStatusLabel(match.stockStatus) }}
                  </mat-chip>
                </td>
              </ng-container>

              <!-- Seller Column -->
              <ng-container matColumnDef="seller">
                <th mat-header-cell *matHeaderCellDef>Seller</th>
                <td mat-cell *matCellDef="let match">
                  <span class="seller-cell">{{ match.sellerName || 'Unknown' }}</span>
                </td>
              </ng-container>

              <!-- Trigger Column -->
              <ng-container matColumnDef="trigger">
                <th mat-header-cell *matHeaderCellDef>Trigger</th>
                <td mat-cell *matCellDef="let match">
                  <div class="trigger-cell">
                    <mat-icon [color]="match.wouldTrigger ? 'primary' : ''" 
                              [matTooltip]="match.triggerReason">
                      {{ match.wouldTrigger ? 'check_circle' : 'cancel' }}
                    </mat-icon>
                  </div>
                </td>
              </ng-container>

              <!-- Notification Column -->
              <ng-container matColumnDef="notification">
                <th mat-header-cell *matHeaderCellDef>Notification</th>
                <td mat-cell *matCellDef="let match">
                  <div class="notification-cell">
                    <mat-chip class="notification-chip" [class]="getNotificationChipClass(match)">
                      <mat-icon>{{ getNotificationIcon(match) }}</mat-icon>
                      {{ getNotificationSummary(match) }}
                    </mat-chip>
                  </div>
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns;" 
                  [class]="row.wouldTrigger ? 'trigger-row' : 'no-trigger-row'"></tr>
            </table>
          </div>

          <!-- No Matches Message -->
          <div class="no-matches" *ngIf="getDisplayedMatches().length === 0">
            <mat-icon>info</mat-icon>
            <p>{{ showOnlyTriggers ? 'No triggers found in the historical data.' : 'No historical data available.' }}</p>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- No Results Message -->
      <mat-card class="no-results-card" appearance="outlined" *ngIf="!results.matches || results.matches.length === 0">
        <mat-card-content>
          <div class="no-results-content">
            <mat-icon class="no-results-icon">info</mat-icon>
            <h3>No Test Data Available</h3>
            <p>{{ getNoResultsMessage() }}</p>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styleUrls: ['./alert-test-results.component.scss']
})
export class AlertTestResultsComponent implements OnInit {
  @Input() results!: AlertTestResultDto;
  @Input() alert!: AlertRuleDto;

  displayedColumns: string[] = ['timestamp', 'price', 'stockStatus', 'seller', 'trigger'];
  showOnlyTriggers = false;

  ngOnInit(): void {
    // Add notification column if notifications are enabled
    if (this.results?.notificationsEnabled) {
      this.displayedColumns = ['timestamp', 'price', 'stockStatus', 'seller', 'trigger', 'notification'];
    }
  }

  getResultIcon(): string {
    return this.results.wouldTrigger ? 'check_circle' : 'cancel';
  }

  getResultIconClass(): string {
    return this.results.wouldTrigger ? 'success-icon' : 'error-icon';
  }

  getResultTitle(): string {
    if (this.results.testType === 'SINGLE_POINT') {
      return this.results.wouldTrigger ? 'Alert Would Trigger' : 'Alert Would Not Trigger';
    } else {
      const triggerCount = this.results.triggeredCount || 0;
      return triggerCount > 0 ? `Found ${triggerCount} Trigger${triggerCount > 1 ? 's' : ''}` : 'No Triggers Found';
    }
  }

  getResultSubtitle(): string {
    if (this.results.testType === 'SINGLE_POINT') {
      return this.results.wouldTrigger 
        ? 'Your alert conditions are met by the test data'
        : 'Your alert conditions are not met by the test data';
    } else {
      return `Analysis of ${this.results.totalPointsTested} historical price points`;
    }
  }

  getTestTypeLabel(): string {
    return this.results.testType === 'SINGLE_POINT' ? 'Quick Test' : 'Historical Analysis';
  }

  getTriggerRate(): string {
    if (!this.results.totalPointsTested) return '0';
    const rate = ((this.results.triggeredCount || 0) / this.results.totalPointsTested) * 100;
    return rate.toFixed(1);
  }

  getTriggerCountClass(): string {
    const count = this.results.triggeredCount || 0;
    if (count === 0) return 'no-triggers';
    if (count <= 5) return 'few-triggers';
    return 'many-triggers';
  }

  getStockStatusLabel(status: string): string {
    switch (status) {
      case 'IN_STOCK': return 'In Stock';
      case 'OUT_OF_STOCK': return 'Out of Stock';
      case 'LIMITED_STOCK': return 'Limited';
      case 'PREORDER': return 'Pre-order';
      default: return status || 'Unknown';
    }
  }

  getStockStatusClass(status: string): string {
    switch (status) {
      case 'IN_STOCK': return 'in-stock';
      case 'OUT_OF_STOCK': return 'out-of-stock';
      case 'LIMITED_STOCK': return 'limited-stock';
      case 'PREORDER': return 'preorder';
      default: return 'unknown-stock';
    }
  }

  getDisplayedMatches(): AlertTestMatchDto[] {
    if (!this.results || !this.results.matches) return [];
    
    if (this.showOnlyTriggers) {
      return this.results.matches.filter(match => match.wouldTrigger);
    }
    
    return this.results.matches;
  }

  toggleTriggerView(): void {
    this.showOnlyTriggers = !this.showOnlyTriggers;
  }

  getNoResultsMessage(): string {
    if (this.results.testType === 'SINGLE_POINT') {
      return 'No test data was generated. Please check your test parameters and try again.';
    } else {
      return 'No historical price data was found for the specified date range. Try expanding your date range or check if the product has price history.';
    }
  }

  getNotificationChipClass(match: AlertTestMatchDto): string {
    return match.notificationError ? 'error-notification-chip' : 'success-notification-chip';
  }

  getNotificationIcon(match: AlertTestMatchDto): string {
    return match.notificationError ? 'error' : 'check_circle';
  }

  getNotificationSummary(match: AlertTestMatchDto): string {
    return match.notificationError ? 'Error' : 'Success';
  }

  getNotificationStatusClass(): string {
    return this.results.notificationsEnabled && this.results.overallNotificationStatus === 'Active' ? 'active-notification' : 'inactive-notification';
  }

  getMatchNotificationChipClass(match: AlertTestMatchDto): string {
    return match.notificationError ? 'error-notification-chip' : 'success-notification-chip';
  }

  getMatchNotificationIcon(match: AlertTestMatchDto): string {
    return match.notificationError ? 'error' : 'check_circle';
  }

  getMatchNotificationStatus(match: AlertTestMatchDto): string {
    return match.notificationError ? 'Error' : 'Success';
  }

  getSummaryNotificationChipClass(): string {
    return this.results.notificationsEnabled && this.results.overallNotificationStatus === 'Active' ? 'active-notification' : 'inactive-notification';
  }

  getSummaryNotificationIcon(): string {
    return this.results.notificationsEnabled && this.results.overallNotificationStatus === 'Active' ? 'check_circle' : 'cancel';
  }

  getSummaryNotificationText(): string {
    return this.results.notificationsEnabled ? 'Active' : 'Inactive';
  }
} 