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
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { firstValueFrom } from 'rxjs';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import {
  ProxyConfigurationDto,
  ProxyStatsDto,
  BulkProxyTestDto,
  BulkProxyActiveStatusDto,
  TechTickerApiClient
} from '../../../shared/api/api-client';

@Component({
  selector: 'app-proxy-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDialogModule,
    MatCheckboxModule,
    MatTooltipModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ScrollingModule,
    HasPermissionDirective
  ],
  templateUrl: './proxy-list.component.html',
  styleUrls: ['./proxy-list.component.scss']
})
export class ProxyListComponent implements OnInit {
  proxies: ProxyConfigurationDto[] = [];
  filteredProxies: ProxyConfigurationDto[] = [];
  stats: ProxyStatsDto | null = null;
  loading = false;

  // Virtual scrolling configuration
  readonly itemSize = 72; // Height of each proxy row in pixels
  readonly minBufferPx = 200; // Minimum buffer size
  readonly maxBufferPx = 400; // Maximum buffer size

  // Selection functionality
  selectedProxies = new Set<string>();
  selectAll = false;

  // Bulk operations
  bulkTesting = false;
  bulkUpdating = false;

  // Filtering functionality
  filterText = '';
  filterType = '';
  filterStatus = '';

  displayedColumns: string[] = [
    'select',
    'displayName',
    'proxyType',
    'status',
    'successRate',
    'totalRequests',
    'lastTested',
    'actions'
  ];

  // Filter options
  typeOptions = [
    { value: '', label: 'All Types' },
    { value: 'HTTP', label: 'HTTP' },
    { value: 'HTTPS', label: 'HTTPS' },
    { value: 'SOCKS4', label: 'SOCKS4' },
    { value: 'SOCKS5', label: 'SOCKS5' }
  ];

  statusOptions = [
    { value: '', label: 'All Status' },
    { value: 'active', label: 'Active' },
    { value: 'inactive', label: 'Inactive' },
    { value: 'healthy', label: 'Healthy' },
    { value: 'unhealthy', label: 'Unhealthy' }
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
        this.applyFilters();
      } else {
        this.proxies = [];
        this.filteredProxies = [];
        this.snackBar.open('No proxies found', 'Close', { duration: 3000 });
      }
    } catch (error) {
      console.error('Failed to load proxies:', error);
      this.snackBar.open('Failed to load proxies', 'Close', { duration: 3000 });
      this.proxies = [];
      this.filteredProxies = [];
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
    return this.filteredProxies.length > 50; // Use virtual scrolling for 50+ items
  }

  // Get viewport height for virtual scrolling
  getVirtualScrollHeight(): number {
    const maxHeight = 600; // Maximum viewport height
    const calculatedHeight = Math.min(this.filteredProxies.length * this.itemSize + 20, maxHeight);
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

  // Filtering methods
  applyFilters(): void {
    this.filteredProxies = this.proxies.filter(proxy => {
      // Text filter (host, port, or display name)
      const textMatch = !this.filterText ||
        proxy.displayName?.toLowerCase().includes(this.filterText.toLowerCase()) ||
        proxy.host?.toLowerCase().includes(this.filterText.toLowerCase()) ||
        proxy.port?.toString().includes(this.filterText);

      // Type filter
      const typeMatch = !this.filterType || proxy.proxyType === this.filterType;

      // Status filter
      let statusMatch = true;
      if (this.filterStatus) {
        switch (this.filterStatus) {
          case 'active':
            statusMatch = proxy.isActive ?? false;
            break;
          case 'inactive':
            statusMatch = !(proxy.isActive ?? false);
            break;
          case 'healthy':
            statusMatch = proxy.isHealthy ?? false;
            break;
          case 'unhealthy':
            statusMatch = !(proxy.isHealthy ?? false);
            break;
        }
      }

      return textMatch && typeMatch && statusMatch;
    });
  }

  onFilterChange(): void {
    this.applyFilters();
    this.updateSelectAllState();
  }

  clearFilters(): void {
    this.filterText = '';
    this.filterType = '';
    this.filterStatus = '';
    this.applyFilters();
  }

  // Selection methods
  toggleProxySelection(proxy: ProxyConfigurationDto): void {
    const proxyId = proxy.proxyConfigurationId!;
    if (this.selectedProxies.has(proxyId)) {
      this.selectedProxies.delete(proxyId);
    } else {
      this.selectedProxies.add(proxyId);
    }
    this.updateSelectAllState();
  }

  isProxySelected(proxy: ProxyConfigurationDto): boolean {
    return this.selectedProxies.has(proxy.proxyConfigurationId!);
  }

  toggleSelectAll(): void {
    if (this.selectAll) {
      this.selectedProxies.clear();
    } else {
      this.filteredProxies.forEach(proxy => {
        this.selectedProxies.add(proxy.proxyConfigurationId!);
      });
    }
    this.selectAll = !this.selectAll;
  }

  updateSelectAllState(): void {
    const totalProxies = this.filteredProxies.length;
    const selectedCount = this.getSelectedCount();
    this.selectAll = totalProxies > 0 && selectedCount === totalProxies;
  }

  getSelectedCount(): number {
    return Array.from(this.selectedProxies).filter(id =>
      this.filteredProxies.some(p => p.proxyConfigurationId === id)
    ).length;
  }

  // Bulk operations
  async bulkTestProxies(): Promise<void> {
    const selectedIds = Array.from(this.selectedProxies).filter(id =>
      this.filteredProxies.some(p => p.proxyConfigurationId === id)
    );

    if (selectedIds.length === 0) {
      this.snackBar.open('No proxies selected for testing', 'Close', { duration: 3000 });
      return;
    }

    this.bulkTesting = true;
    try {
      const testDto = new BulkProxyTestDto({
        proxyIds: selectedIds,
        testUrl: 'https://httpbin.org/ip',
        timeoutSeconds: 30
      });

      const response = await firstValueFrom(
        this.apiClient.bulkTestProxies(testDto)
      );

      if (response?.success && response.data) {
        const results = response.data;
        const successCount = results.filter(r => r.isHealthy).length;
        const failCount = results.length - successCount;

        this.snackBar.open(
          `Bulk test completed: ${successCount} successful, ${failCount} failed`,
          'Close',
          { duration: 5000 }
        );

        // Reload data to show updated test results
        await this.loadProxies();
        await this.loadStats();
      } else {
        this.snackBar.open('Bulk test failed', 'Close', { duration: 3000 });
      }
    } catch (error) {
      console.error('Error bulk testing proxies:', error);
      this.snackBar.open('Failed to test proxies', 'Close', { duration: 3000 });
    } finally {
      this.bulkTesting = false;
    }
  }

  async bulkToggleStatus(isActive: boolean): Promise<void> {
    const selectedIds = Array.from(this.selectedProxies).filter(id =>
      this.filteredProxies.some(p => p.proxyConfigurationId === id)
    );

    if (selectedIds.length === 0) {
      const action = isActive ? 'enable' : 'disable';
      this.snackBar.open(`No proxies selected to ${action}`, 'Close', { duration: 3000 });
      return;
    }

    this.bulkUpdating = true;
    try {
      const statusDto = new BulkProxyActiveStatusDto({
        proxyIds: selectedIds,
        isActive: isActive
      });

      const response = await firstValueFrom(
        this.apiClient.bulkSetProxyActiveStatus(statusDto)
      );

      if (response?.success) {
        const action = isActive ? 'enabled' : 'disabled';
        this.snackBar.open(
          `${selectedIds.length} proxies ${action} successfully`,
          'Close',
          { duration: 3000 }
        );

        // Update local data
        this.proxies.forEach(proxy => {
          if (selectedIds.includes(proxy.proxyConfigurationId!)) {
            proxy.isActive = isActive;
          }
        });

        this.applyFilters();
        await this.loadStats();
      } else {
        const action = isActive ? 'enable' : 'disable';
        this.snackBar.open(`Failed to ${action} proxies`, 'Close', { duration: 3000 });
      }
    } catch (error) {
      console.error('Error updating proxy status:', error);
      const action = isActive ? 'enable' : 'disable';
      this.snackBar.open(`Failed to ${action} proxies`, 'Close', { duration: 3000 });
    } finally {
      this.bulkUpdating = false;
    }
  }

  async bulkDeleteProxies(): Promise<void> {
    const selectedIds = Array.from(this.selectedProxies).filter(id =>
      this.filteredProxies.some(p => p.proxyConfigurationId === id)
    );

    if (selectedIds.length === 0) {
      this.snackBar.open('No proxies selected for deletion', 'Close', { duration: 3000 });
      return;
    }

    if (!confirm(`Are you sure you want to delete ${selectedIds.length} selected proxies? This action cannot be undone.`)) {
      return;
    }

    this.bulkUpdating = true;
    let successCount = 0;
    let failCount = 0;

    try {
      // Delete proxies one by one since there's no bulk delete API
      for (const proxyId of selectedIds) {
        try {
          const response = await firstValueFrom(
            this.apiClient.deleteProxy(proxyId)
          );

          if (response?.success) {
            successCount++;
            // Remove from local arrays
            this.proxies = this.proxies.filter(p => p.proxyConfigurationId !== proxyId);
            this.selectedProxies.delete(proxyId);
          } else {
            failCount++;
          }
        } catch (error) {
          console.error(`Error deleting proxy ${proxyId}:`, error);
          failCount++;
        }
      }

      this.snackBar.open(
        `Bulk delete completed: ${successCount} deleted, ${failCount} failed`,
        'Close',
        { duration: 5000 }
      );

      this.applyFilters();
      this.updateSelectAllState();
      await this.loadStats();
    } catch (error) {
      console.error('Error bulk deleting proxies:', error);
      this.snackBar.open('Failed to delete proxies', 'Close', { duration: 3000 });
    } finally {
      this.bulkUpdating = false;
    }
  }

  clearSelection(): void {
    this.selectedProxies.clear();
    this.selectAll = false;
  }
}
