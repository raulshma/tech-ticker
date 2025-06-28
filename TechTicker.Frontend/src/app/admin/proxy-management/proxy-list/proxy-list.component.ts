import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatDialogModule } from '@angular/material/dialog';
// TODO: Replace with actual API client imports when generated
// import { ProxyConfigurationDto, ProxyStatsDto } from '../../../shared/api/api-client';

// Temporary interfaces until API client is generated
interface ProxyConfigurationDto {
  proxyConfigurationId: string;
  host: string;
  port: number;
  proxyType: string;
  username?: string | null;
  hasPassword: boolean;
  description?: string | null;
  isActive: boolean;
  isHealthy: boolean;
  lastTestedAt?: string | null;
  lastUsedAt?: string | null;
  successRate: number;
  totalRequests: number;
  successfulRequests: number;
  failedRequests: number;
  consecutiveFailures: number;
  timeoutSeconds: number;
  maxRetries: number;
  lastErrorMessage?: string | null;
  lastErrorCode?: string | null;
  createdAt: string;
  updatedAt: string;
  displayName: string;
  requiresAuthentication: boolean;
  isReliable: boolean;
  statusDescription: string;
}

interface ProxyStatsDto {
  totalProxies: number;
  activeProxies: number;
  healthyProxies: number;
  averageSuccessRate: number;
  proxiesWithErrors: number;
  proxiesByType: { [key: string]: number };
  proxiesByStatus: { [key: string]: number };
}

@Component({
  selector: 'app-proxy-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDialogModule
  ],
  templateUrl: './proxy-list.component.html',
  styleUrls: ['./proxy-list.component.scss']
})
export class ProxyListComponent implements OnInit {
  proxies: ProxyConfigurationDto[] = [];
  stats: ProxyStatsDto | null = null;
  loading = false;
  displayedColumns: string[] = [
    'displayName',
    'proxyType',
    'status',
    'successRate',
    'totalRequests',
    'lastTested',
    'actions'
  ];

  constructor(
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadProxies();
    this.loadStats();
  }

  async loadProxies(): Promise<void> {
    this.loading = true;
    try {
      // TODO: Replace with actual API call when client is generated
      // this.proxies = await this.proxyService.getAllProxies();

      // Mock data for demonstration
      this.proxies = [
        {
          proxyConfigurationId: '1',
          host: '192.168.1.100',
          port: 8080,
          proxyType: 'HTTP',
          username: 'user1',
          hasPassword: true,
          description: 'Primary HTTP proxy',
          isActive: true,
          isHealthy: true,
          lastTestedAt: new Date().toISOString(),
          lastUsedAt: new Date().toISOString(),
          successRate: 95.5,
          totalRequests: 1250,
          successfulRequests: 1194,
          failedRequests: 56,
          consecutiveFailures: 0,
          timeoutSeconds: 30,
          maxRetries: 3,
          lastErrorMessage: null,
          lastErrorCode: null,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          displayName: 'HTTP://192.168.1.100:8080',
          requiresAuthentication: true,
          isReliable: true,
          statusDescription: 'Excellent'
        } as ProxyConfigurationDto,
        {
          proxyConfigurationId: '2',
          host: '10.0.0.50',
          port: 1080,
          proxyType: 'SOCKS5',
          username: null,
          hasPassword: false,
          description: 'SOCKS5 proxy for testing',
          isActive: true,
          isHealthy: false,
          lastTestedAt: new Date(Date.now() - 3600000).toISOString(),
          lastUsedAt: new Date(Date.now() - 1800000).toISOString(),
          successRate: 45.2,
          totalRequests: 890,
          successfulRequests: 402,
          failedRequests: 488,
          consecutiveFailures: 5,
          timeoutSeconds: 30,
          maxRetries: 3,
          lastErrorMessage: 'Connection timeout',
          lastErrorCode: 'TIMEOUT',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          displayName: 'SOCKS5://10.0.0.50:1080',
          requiresAuthentication: false,
          isReliable: false,
          statusDescription: 'Poor'
        } as ProxyConfigurationDto
      ];
    } catch (error) {
      this.snackBar.open('Failed to load proxies', 'Close', { duration: 3000 });
    } finally {
      this.loading = false;
    }
  }

  async loadStats(): Promise<void> {
    try {
      // TODO: Replace with actual API call when client is generated
      // this.stats = await this.proxyService.getStats();

      // Mock data for demonstration
      this.stats = {
        totalProxies: 2,
        activeProxies: 2,
        healthyProxies: 1,
        averageSuccessRate: 70.35,
        proxiesWithErrors: 1,
        proxiesByType: { 'HTTP': 1, 'SOCKS5': 1 },
        proxiesByStatus: { 'Excellent': 1, 'Poor': 1 }
      } as ProxyStatsDto;
    } catch (error) {
      console.error('Failed to load proxy stats:', error);
    }
  }

  async testProxy(proxy: ProxyConfigurationDto): Promise<void> {
    try {
      this.snackBar.open(`Testing proxy ${proxy.displayName}...`, 'Close', { duration: 2000 });

      // TODO: Replace with actual API call when client is generated
      // await this.proxyService.testProxy(proxy.proxyConfigurationId);

      // Mock success
      await new Promise(resolve => setTimeout(resolve, 2000));
      this.snackBar.open(`Proxy test completed for ${proxy.displayName}`, 'Close', { duration: 3000 });

      // Reload data
      await this.loadProxies();
      await this.loadStats();
    } catch (error) {
      this.snackBar.open(`Failed to test proxy ${proxy.displayName}`, 'Close', { duration: 3000 });
    }
  }

  async toggleProxyStatus(proxy: ProxyConfigurationDto): Promise<void> {
    try {
      const newStatus = !proxy.isActive;

      // TODO: Replace with actual API call when client is generated
      // await this.proxyService.setActiveStatus(proxy.proxyConfigurationId, newStatus);

      proxy.isActive = newStatus;
      this.snackBar.open(
        `Proxy ${newStatus ? 'enabled' : 'disabled'} successfully`,
        'Close',
        { duration: 3000 }
      );
    } catch (error) {
      this.snackBar.open('Failed to update proxy status', 'Close', { duration: 3000 });
    }
  }

  async deleteProxy(proxy: ProxyConfigurationDto): Promise<void> {
    if (confirm(`Are you sure you want to delete proxy ${proxy.displayName}?`)) {
      try {
        // TODO: Replace with actual API call when client is generated
        // await this.proxyService.deleteProxy(proxy.proxyConfigurationId);

        this.proxies = this.proxies.filter(p => p.proxyConfigurationId !== proxy.proxyConfigurationId);
        this.snackBar.open('Proxy deleted successfully', 'Close', { duration: 3000 });

        await this.loadStats();
      } catch (error) {
        this.snackBar.open('Failed to delete proxy', 'Close', { duration: 3000 });
      }
    }
  }

  getStatusColor(proxy: ProxyConfigurationDto): string {
    if (!proxy.isActive) return 'warn';
    if (!proxy.isHealthy) return 'warn';
    if (proxy.successRate >= 90) return 'primary';
    if (proxy.successRate >= 70) return 'accent';
    return 'warn';
  }

  formatLastTested(dateString: string | null): string {
    if (!dateString) return 'Never';
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffMins < 1440) return `${Math.floor(diffMins / 60)}h ago`;
    return `${Math.floor(diffMins / 1440)}d ago`;
  }
}
