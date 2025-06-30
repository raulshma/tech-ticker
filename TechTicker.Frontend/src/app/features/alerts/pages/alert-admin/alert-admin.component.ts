import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
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
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';

import { AlertsService } from '../../services/alerts.service';
import { AlertRuleDto, UpdateAlertRuleDto } from '../../../../shared/api/api-client';

@Component({
  selector: 'app-alert-admin',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
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
    MatDividerModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule
  ],
  templateUrl: './alert-admin.component.html',
  styleUrls: ['./alert-admin.component.scss']
})
export class AlertAdminComponent implements OnInit {
  alerts: AlertRuleDto[] = [];
  filteredAlerts: AlertRuleDto[] = [];
  displayedColumns: string[] = ['user', 'product', 'condition', 'status', 'created', 'lastTriggered', 'actions'];
  totalAlerts = 0;
  pageSize = 25;
  currentPage = 0;
  isLoading = false;
  isUpdating = false;

  searchControl = new FormControl('');
  statusFilter = new FormControl('');
  conditionFilter = new FormControl('');

  stats: any = null;

  constructor(
    private alertsService: AlertsService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadData();
    this.setupFilters();
  }

  private setupFilters(): void {
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => this.applyFilters());

    this.statusFilter.valueChanges.subscribe(() => this.applyFilters());
    this.conditionFilter.valueChanges.subscribe(() => this.applyFilters());
  }

  loadData(): void {
    this.refreshData();
  }

  refreshData(): void {
    this.isLoading = true;
    this.alertsService.getAllAlerts().subscribe({
      next: (alerts: AlertRuleDto[]) => {
        this.alerts = alerts;
        this.totalAlerts = alerts.length;
        this.calculateStats();
        this.applyFilters();
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading alerts:', error);
        this.snackBar.open('Failed to load alerts', 'Close', { duration: 3000 });
        this.isLoading = false;
      }
    });
  }

  private calculateStats(): void {
    this.stats = {
      totalAlerts: this.alerts.length,
      activeAlerts: this.alerts.filter(a => a.isActive).length,
      triggeredToday: this.getTriggeredTodayCount(),
      filteredResults: this.filteredAlerts.length,
      uniqueUsers: this.getUniqueUsersCount(),
      conditionTypeBreakdown: this.getConditionTypeBreakdown(),
      alertsByStatus: this.getAlertsByStatus()
    };
  }

  private getUniqueUsersCount(): number {
    const uniqueUserEmails = new Set(this.alerts.map(alert => alert.user?.email).filter(email => email));
    return uniqueUserEmails.size;
  }

  private getConditionTypeBreakdown(): { [key: string]: number } {
    return this.alerts.reduce((breakdown, alert) => {
      const type = alert.conditionType || 'UNKNOWN';
      breakdown[type] = (breakdown[type] || 0) + 1;
      return breakdown;
    }, {} as { [key: string]: number });
  }

  private getAlertsByStatus(): { active: number; inactive: number } {
    return {
      active: this.alerts.filter(a => a.isActive).length,
      inactive: this.alerts.filter(a => !a.isActive).length
    };
  }

  getTopConditionType(): string {
    const breakdown = this.stats?.conditionTypeBreakdown || {};
    const topType = Object.entries(breakdown).reduce((max, [type, count]) => 
      (count as number) > max.count ? { type, count: count as number } : max, { type: '', count: 0 });
    return topType.type || 'None';
  }

  getFilterSummary(): string {
    const filters: string[] = [];
    
    if (this.searchControl.value) {
      filters.push(`Search: "${this.searchControl.value}"`);
    }
    
    if (this.statusFilter.value) {
      filters.push(`Status: ${this.statusFilter.value}`);
    }
    
    if (this.conditionFilter.value) {
      filters.push(`Condition: ${this.conditionFilter.value}`);
    }
    
    return filters.length > 0 ? filters.join(', ') : 'No filters applied';
  }

  getActiveAlertsCount(): number {
    return this.alerts.filter(alert => alert.isActive).length;
  }

  getTriggeredTodayCount(): number {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return this.alerts.filter(alert => 
      alert.lastNotifiedAt && new Date(alert.lastNotifiedAt) >= today
    ).length;
  }

  applyFilters(): void {
    let filtered = [...this.alerts];

    const searchTerm = this.searchControl.value?.toLowerCase() || '';
    if (searchTerm) {
      filtered = filtered.filter(alert =>
        alert.product?.name?.toLowerCase().includes(searchTerm) ||
        alert.user?.email?.toLowerCase().includes(searchTerm) ||
        alert.user?.firstName?.toLowerCase().includes(searchTerm) ||
        alert.user?.lastName?.toLowerCase().includes(searchTerm) ||
        alert.ruleDescription?.toLowerCase().includes(searchTerm)
      );
    }

    const statusFilter = this.statusFilter.value;
    if (statusFilter) {
      filtered = filtered.filter(alert => 
        statusFilter === 'active' ? alert.isActive : !alert.isActive
      );
    }

    const conditionFilter = this.conditionFilter.value;
    if (conditionFilter) {
      filtered = filtered.filter(alert => alert.conditionType === conditionFilter);
    }

    this.filteredAlerts = filtered;
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.statusFilter.setValue('');
    this.conditionFilter.setValue('');
  }

  onPageChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.currentPage = event.pageIndex;
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
        this.applyFilters();
        this.calculateStats();
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

  viewAlert(alert: AlertRuleDto): void {
    this.snackBar.open('Alert details view coming soon', 'Close', { duration: 2000 });
  }

  editAlert(alert: AlertRuleDto): void {
    this.snackBar.open('Alert edit functionality coming soon', 'Close', { duration: 2000 });
  }

  testAlert(alert: AlertRuleDto): void {
    this.snackBar.open('Alert testing functionality coming soon', 'Close', { duration: 2000 });
  }

  deleteAlert(alert: AlertRuleDto): void {
    if (confirm(`Are you sure you want to delete the alert for "${alert.product?.name}"?`)) {
      this.alertsService.deleteAlert(alert.alertRuleId!).subscribe({
        next: () => {
          this.alerts = this.alerts.filter(a => a.alertRuleId !== alert.alertRuleId);
          this.applyFilters();
          this.calculateStats();
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
