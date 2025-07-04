import { Component, OnInit } from '@angular/core';
import { AuthService, CurrentUser } from '../../../../shared/services/auth.service';
import { DashboardService, DashboardStats } from '../../services/dashboard.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  standalone: false
})
export class DashboardComponent implements OnInit {
  currentUser: CurrentUser | null = null;
  isAdmin = false;
  isLoading = false;
  error: string | null = null;

  // Dashboard stats from API
  dashboardStats: DashboardStats = {
    totalProducts: 0,
    totalCategories: 0,
    activeMappings: 0,
    activeAlerts: 0,
    totalUsers: 0,
    totalProxies: 0,
    healthyProxies: 0,
    proxyHealthPercentage: 0,
    recentScraperRuns: 0,
    scraperSuccessRate: 0,
    recentNotifications: 0,
    notificationSuccessRate: 0,
    systemHealthy: false,
    recentAlerts: 0
  };

  constructor(
    private authService: AuthService,
    private dashboardService: DashboardService
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      this.isAdmin = this.authService.isAdmin();
    });

    this.loadDashboardStats();
  }

  loadDashboardStats(): void {
    this.isLoading = true;
    this.error = null;

    this.dashboardService.getDashboardStats().subscribe({
      next: (stats) => {
        this.dashboardStats = stats;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading dashboard stats:', error);
        this.error = 'Failed to load dashboard statistics. Please try again.';
        this.isLoading = false;

        // Set default values on error
        this.dashboardStats = {
          totalProducts: 0,
          totalCategories: 0,
          activeMappings: 0,
          activeAlerts: 0,
          totalUsers: 0,
          totalProxies: 0,
          healthyProxies: 0,
          proxyHealthPercentage: 0,
          recentScraperRuns: 0,
          scraperSuccessRate: 0,
          recentNotifications: 0,
          notificationSuccessRate: 0,
          systemHealthy: false,
          recentAlerts: 0
        };
      }
    });
  }

  onCardKeydown(event: KeyboardEvent): void {
    const target = event.target as HTMLElement | null;
    if (!target) return;
    if (event.code === 'Enter' || event.key === 'Enter') {
      target.click();
    } else if (event.code === 'Space' || event.key === ' ' || event.key === 'Spacebar') {
      target.click();
      event.preventDefault();
    }
  }
}
