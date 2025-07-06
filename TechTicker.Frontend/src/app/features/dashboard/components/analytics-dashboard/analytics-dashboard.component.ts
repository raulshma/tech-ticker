import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { AuthService, CurrentUser } from '../../../../shared/services/auth.service';
import { DashboardService, AnalyticsDashboardData } from '../../services/dashboard.service';

@Component({
  selector: 'app-analytics-dashboard',
  templateUrl: './analytics-dashboard.component.html',
  styleUrls: ['./analytics-dashboard.component.scss'],
  standalone: false
})
export class AnalyticsDashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  
  currentUser: CurrentUser | null = null;
  isAdmin = false;
  isLoading = false;
  error: string | null = null;

  // Analytics data
  analyticsData: AnalyticsDashboardData | null = null;
  
  // Date range for analytics
  dateFrom: Date | null = null;
  dateTo: Date | null = null;
  
  // Active tab
  activeTab = 'overview';

  constructor(
    private authService: AuthService,
    private dashboardService: DashboardService
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.pipe(
      takeUntil(this.destroy$)
    ).subscribe((user: CurrentUser | null) => {
      this.currentUser = user;
      this.isAdmin = this.authService.isAdmin();
    });

    // Set default date range (last 30 days)
    this.dateTo = new Date();
    this.dateFrom = new Date();
    this.dateFrom.setDate(this.dateFrom.getDate() - 30);

    this.loadAnalyticsData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadAnalyticsData(): void {
    this.isLoading = true;
    this.error = null;

    this.dashboardService.getAnalyticsDashboard(this.dateFrom, this.dateTo).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (data) => {
        this.analyticsData = data;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading analytics data:', error);
        this.error = 'Failed to load analytics data. Please try again.';
        this.isLoading = false;
      }
    });
  }

  onDateRangeChange(): void {
    this.loadAnalyticsData();
  }

  onTabChange(tab: string): void {
    this.activeTab = tab;
  }

  getSystemHealthClass(): string {
    if (!this.analyticsData?.realTimeStatus.systemHealthy) {
      return 'text-danger';
    }
    return 'text-success';
  }

  getSystemHealthIcon(): string {
    if (!this.analyticsData?.realTimeStatus.systemHealthy) {
      return 'warning';
    }
    return 'check_circle';
  }
} 