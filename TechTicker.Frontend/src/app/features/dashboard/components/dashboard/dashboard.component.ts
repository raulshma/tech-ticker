import { Component, OnInit } from '@angular/core';
import { AuthService, CurrentUser } from '../../../../shared/services/auth.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  standalone: false
})
export class DashboardComponent implements OnInit {
  currentUser: CurrentUser | null = null;
  isAdmin = false;

  // Mock data for dashboard stats
  dashboardStats = {
    totalProducts: 0,
    totalCategories: 0,
    activeMappings: 0,
    activeAlerts: 0,
    totalUsers: 0
  };

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      this.isAdmin = this.authService.isAdmin();
    });

    // TODO: Load actual dashboard statistics from API
    this.loadDashboardStats();
  }

  private loadDashboardStats(): void {
    // Mock data - replace with actual API calls when backend is ready
    this.dashboardStats = {
      totalProducts: 156,
      totalCategories: 12,
      activeMappings: 342,
      activeAlerts: 28,
      totalUsers: 15
    };
  }
}
