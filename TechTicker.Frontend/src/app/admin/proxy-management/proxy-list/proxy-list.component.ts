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
import { ScrollingModule } from '@angular/cdk/scrolling';
import { firstValueFrom } from 'rxjs';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import {
  ProxyConfigurationDto,
  ProxyStatsDto,
  TechTickerApiClient
} from '../../../shared/api/api-client';

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
    MatDialogModule,
    ScrollingModule,
    HasPermissionDirective
  ],
  templateUrl: './proxy-list.component.html',
  styleUrls: ['./proxy-list.component.scss']
})
export class ProxyListComponent implements OnInit {
  proxies: ProxyConfigurationDto[] = [];
  stats: ProxyStatsDto | null = null;
  loading = false;

  // Virtual scrolling configuration
  readonly itemSize = 72; // Height of each proxy row in pixels
  readonly minBufferPx = 200; // Minimum buffer size
  readonly maxBufferPx = 400; // Maximum buffer size

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
    private snackBar: MatSnackBar,
    private apiClient: TechTickerApiClient
  ) {}

  ngOnInit(): void {
    this.loadProxies();
    this.loadStats();
  }

  async loadProxies(): Promise<void> {
    this.loading = true;
    try {
      const response = await firstValueFrom(this.apiClient.getAllProxies());
      if (response?.success && response.data) {
        this.proxies = response.data;
      } else {
        this.proxies = [];
        this.snackBar.open('No proxies found', 'Close', { duration: 3000 });
      }
    } catch (error) {
      console.error('Failed to load proxies:', error);
      this.snackBar.open('Failed to load proxies', 'Close', { duration: 3000 });
      this.proxies = [];
    } finally {
      this.loading = false;
    }
  }

  async loadStats(): Promise<void> {
    try {
      const response = await firstValueFrom(this.apiClient.getProxyStats());
      if (response?.success && response.data) {
        this.stats = response.data;
      } else {
        this.stats = null;
      }
    } catch (error) {
      console.error('Failed to load proxy stats:', error);
      this.stats = null;
    }
  }

  async testProxy(proxy: ProxyConfigurationDto): Promise<void> {
    try {
      this.snackBar.open(`Testing proxy ${proxy.displayName}...`, 'Close', { duration: 2000 });

      const response = await firstValueFrom(
        this.apiClient.testProxy(proxy.proxyConfigurationId!, undefined, 30)
      );

      if (response?.success && response.data) {
        const result = response.data;
        if (result.isHealthy) {
          this.snackBar.open(
            `Proxy test successful for ${proxy.displayName} (${result.responseTimeMs}ms)`,
            'Close',
            { duration: 3000 }
          );
        } else {
          this.snackBar.open(
            `Proxy test failed for ${proxy.displayName}: ${result.errorMessage}`,
            'Close',
            { duration: 5000 }
          );
        }
      } else {
        this.snackBar.open(`Proxy test completed for ${proxy.displayName}`, 'Close', { duration: 3000 });
      }

      // Reload data
      await this.loadProxies();
      await this.loadStats();
    } catch (error) {
      console.error('Error testing proxy:', error);
      this.snackBar.open(`Failed to test proxy ${proxy.displayName}`, 'Close', { duration: 3000 });
    }
  }

  async toggleProxyStatus(proxy: ProxyConfigurationDto): Promise<void> {
    try {
      const newStatus = !proxy.isActive;

      const response = await firstValueFrom(
        this.apiClient.setProxyActiveStatus(proxy.proxyConfigurationId!, newStatus)
      );

      if (response?.success) {
        proxy.isActive = newStatus;
        this.snackBar.open(
          `Proxy ${newStatus ? 'enabled' : 'disabled'} successfully`,
          'Close',
          { duration: 3000 }
        );
        await this.loadStats(); // Refresh stats
      } else {
        this.snackBar.open('Failed to update proxy status', 'Close', { duration: 3000 });
      }
    } catch (error) {
      console.error('Error updating proxy status:', error);
      this.snackBar.open('Failed to update proxy status', 'Close', { duration: 3000 });
    }
  }

  async deleteProxy(proxy: ProxyConfigurationDto): Promise<void> {
    if (confirm(`Are you sure you want to delete proxy ${proxy.displayName}?`)) {
      try {
        const response = await firstValueFrom(
          this.apiClient.deleteProxy(proxy.proxyConfigurationId!)
        );

        if (response?.success) {
          this.proxies = this.proxies.filter(p => p.proxyConfigurationId !== proxy.proxyConfigurationId);
          this.snackBar.open('Proxy deleted successfully', 'Close', { duration: 3000 });
          await this.loadStats();
        } else {
          this.snackBar.open('Failed to delete proxy', 'Close', { duration: 3000 });
        }
      } catch (error) {
        console.error('Error deleting proxy:', error);
        this.snackBar.open('Failed to delete proxy', 'Close', { duration: 3000 });
      }
    }
  }

  getStatusColor(proxy: ProxyConfigurationDto): string {
    if (!proxy.isActive) return 'warn';
    if (!proxy.isHealthy) return 'warn';
    const successRate = proxy.successRate ?? 0;
    if (successRate >= 90) return 'primary';
    if (successRate >= 70) return 'accent';
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

  // TrackBy function for better performance with large lists
  trackByProxyId(_index: number, proxy: ProxyConfigurationDto): string {
    return proxy.proxyConfigurationId || '';
  }

  // Check if virtual scrolling should be used
  shouldUseVirtualScrolling(): boolean {
    return this.proxies.length > 50; // Use virtual scrolling for 50+ items
  }

  // Get viewport height for virtual scrolling
  getVirtualScrollHeight(): number {
    const maxHeight = 600; // Maximum viewport height
    const calculatedHeight = Math.min(this.proxies.length * this.itemSize + 20, maxHeight);
    return Math.max(calculatedHeight, 200); // Minimum height of 200px
  }

  // Expose Math for template
  readonly Math = Math;

  // Get color for proxy type chip
  getProxyTypeColor(proxyType: string | undefined): string {
    switch (proxyType?.toUpperCase()) {
      case 'HTTP':
      case 'HTTPS':
        return 'primary';
      case 'SOCKS4':
        return 'accent';
      case 'SOCKS5':
        return 'warn';
      default:
        return 'basic';
    }
  }
}
