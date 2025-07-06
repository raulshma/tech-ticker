import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { 
  IntegrationsFeaturesService, 
  IntegrationsAndFeaturesDto, 
  FeatureDto, 
  IntegrationDto, 
  FeatureStatus, 
  IntegrationStatus 
} from '../../services/integrations-features.service';

@Component({
  selector: 'app-integrations-features-overview',
  templateUrl: './integrations-features-overview.component.html',
  styleUrls: ['./integrations-features-overview.component.scss'],
  standalone: false
})
export class IntegrationsFeaturesOverviewComponent implements OnInit {
  loading = false;
  overview: IntegrationsAndFeaturesDto | null = null;
  
  // Grouped features and integrations
  featuresByCategory: { [category: string]: FeatureDto[] } = {};
  integrationsByType: { [type: string]: IntegrationDto[] } = {};
  
  // Tab selection
  selectedTabIndex = 0;
  
  // Enums for template
  FeatureStatus = FeatureStatus;
  IntegrationStatus = IntegrationStatus;

  constructor(
    private integrationsService: IntegrationsFeaturesService,
    private snackBar: MatSnackBar,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadOverview();
  }

  async loadOverview(): Promise<void> {
    this.loading = true;
    try {
      const response = await this.integrationsService.getIntegrationsAndFeatures().toPromise();
      if (response?.success) {
        this.overview = response.data;
        this.groupFeaturesByCategory();
        this.groupIntegrationsByType();
      } else {
        this.showError('Failed to load system overview');
      }
    } catch (error) {
      console.error('Error loading overview:', error);
      this.showError('Failed to load system overview');
    } finally {
      this.loading = false;
    }
  }

  private groupFeaturesByCategory(): void {
    if (!this.overview?.features) return;
    
    this.featuresByCategory = {};
    this.overview.features.forEach(feature => {
      const category = feature.category || 'Other';
      if (!this.featuresByCategory[category]) {
        this.featuresByCategory[category] = [];
      }
      this.featuresByCategory[category].push(feature);
    });
  }

  private groupIntegrationsByType(): void {
    if (!this.overview?.integrations) return;
    
    this.integrationsByType = {};
    this.overview.integrations.forEach(integration => {
      const type = integration.type || 'Other';
      if (!this.integrationsByType[type]) {
        this.integrationsByType[type] = [];
      }
      this.integrationsByType[type].push(integration);
    });
  }

  onFeatureClick(feature: FeatureDto): void {
    if (feature.route && feature.isAvailable) {
      // Record feature usage
      if (feature.id) {
        this.integrationsService.recordFeatureUsage(feature.id).subscribe();
      }
      // Navigate to feature
      this.router.navigate([feature.route]);
    } else if (!feature.isAvailable) {
      this.showError(feature.unavailableReason || 'Feature not available');
    }
  }

  onIntegrationConfigure(integration: IntegrationDto): void {
    if (integration.configurationRoute) {
      this.router.navigate([integration.configurationRoute]);
    } else {
      this.showError('Configuration not available for this integration');
    }
  }

  async onIntegrationHealthCheck(integration: IntegrationDto): Promise<void> {
    if (!integration.id) {
      this.showError('Integration ID not available');
      return;
    }

    try {
      const response = await this.integrationsService.checkIntegrationHealth(integration.id).toPromise();
      if (response?.success && response.data) {
        this.showSuccess(`Health check completed: ${response.data.message || 'Health check successful'}`);
        // Refresh the overview to get updated health status
        await this.loadOverview();
      } else {
        this.showError('Health check failed');
      }
    } catch (error) {
      console.error('Error during health check:', error);
      this.showError('Health check failed');
    }
  }

  async onRefreshAllHealth(): Promise<void> {
    try {
      const response = await this.integrationsService.refreshIntegrationHealth().toPromise();
      if (response?.success) {
        this.showSuccess('All integration health checks refreshed');
        await this.loadOverview();
      } else {
        this.showError('Failed to refresh health checks');
      }
    } catch (error) {
      console.error('Error refreshing health:', error);
      this.showError('Failed to refresh health checks');
    }
  }

  getFeatureStatusColor(status: FeatureStatus): string {
    switch (status) {
      case FeatureStatus.Active:
        return 'primary';
      case FeatureStatus.Inactive:
        return 'basic';
      case FeatureStatus.NeedsConfiguration:
        return 'warn';
      case FeatureStatus.Disabled:
        return 'basic';
      case FeatureStatus.Unavailable:
        return 'basic';
      default:
        return 'basic';
    }
  }

  getIntegrationStatusColor(status: IntegrationStatus): string {
    switch (status) {
      case IntegrationStatus.Connected:
        return 'primary';
      case IntegrationStatus.Disconnected:
        return 'basic';
      case IntegrationStatus.NeedsConfiguration:
        return 'warn';
      case IntegrationStatus.Error:
        return 'warn';
      case IntegrationStatus.Disabled:
        return 'basic';
      default:
        return 'basic';
    }
  }

  getHealthScoreColor(score: number): string {
    if (score >= 80) return 'primary';
    if (score >= 60) return 'accent';
    return 'warn';
  }

  getCategoryKeys(): string[] {
    return Object.keys(this.featuresByCategory);
  }

  getIntegrationTypeKeys(): string[] {
    return Object.keys(this.integrationsByType);
  }

  // Helper methods for safe enum comparisons
  isFeatureStatusEqual(status: any, target: FeatureStatus): boolean {
    if (status === undefined || status === null) return false;
    
    // Convert numeric values to our enum
    if (typeof status === 'number') {
      switch (status) {
        case 0: return target === FeatureStatus.Active;
        case 1: return target === FeatureStatus.Inactive;
        case 2: return target === FeatureStatus.NeedsConfiguration;
        case 3: return target === FeatureStatus.Disabled;
        case 4: return target === FeatureStatus.Unavailable;
        default: return false;
      }
    }
    
    // Handle string values
    return status === target;
  }

  isIntegrationStatusEqual(status: any, target: IntegrationStatus): boolean {
    if (status === undefined || status === null) return false;
    
    // Convert numeric values to our enum
    if (typeof status === 'number') {
      switch (status) {
        case 0: return target === IntegrationStatus.Connected;
        case 1: return target === IntegrationStatus.Disconnected;
        case 2: return target === IntegrationStatus.NeedsConfiguration;
        case 3: return target === IntegrationStatus.Error;
        case 4: return target === IntegrationStatus.Disabled;
        default: return false;
      }
    }
    
    // Handle string values
    return status === target;
  }

  // Safe enum conversion methods
  getFeatureStatusSafe(status: any): FeatureStatus {
    if (status === undefined || status === null) return FeatureStatus.Inactive;
    
    if (typeof status === 'number') {
      switch (status) {
        case 0: return FeatureStatus.Active;
        case 1: return FeatureStatus.Inactive;
        case 2: return FeatureStatus.NeedsConfiguration;
        case 3: return FeatureStatus.Disabled;
        case 4: return FeatureStatus.Unavailable;
        default: return FeatureStatus.Inactive;
      }
    }
    
    return status as FeatureStatus;
  }

  getIntegrationStatusSafe(status: any): IntegrationStatus {
    if (status === undefined || status === null) return IntegrationStatus.Disconnected;
    
    if (typeof status === 'number') {
      switch (status) {
        case 0: return IntegrationStatus.Connected;
        case 1: return IntegrationStatus.Disconnected;
        case 2: return IntegrationStatus.NeedsConfiguration;
        case 3: return IntegrationStatus.Error;
        case 4: return IntegrationStatus.Disabled;
        default: return IntegrationStatus.Disconnected;
      }
    }
    
    return status as IntegrationStatus;
  }

  private showSuccess(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: ['success-snackbar']
    });
  }

  private showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }
} 