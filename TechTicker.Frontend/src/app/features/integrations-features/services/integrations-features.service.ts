import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { 
  TechTickerApiClient,
  IntegrationsAndFeaturesDto,
  FeatureDto,
  IntegrationDto,
  SystemHealthDto,
  IntegrationHealthCheckDto,
  ConfigurationGuideDto,
  FeatureUsageDto,
  ConfigurationStepDto,
  FeatureStatus as ApiFeatureStatus,
  IntegrationStatus as ApiIntegrationStatus
} from '../../../shared/api/api-client';

// Define local enums with proper string values for component use
export enum FeatureStatus {
  Active = 'Active',
  Inactive = 'Inactive',
  NeedsConfiguration = 'NeedsConfiguration',
  Disabled = 'Disabled',
  Unavailable = 'Unavailable'
}

export enum IntegrationStatus {
  Connected = 'Connected',
  Disconnected = 'Disconnected',
  NeedsConfiguration = 'NeedsConfiguration',
  Error = 'Error',
  Disabled = 'Disabled'
}

// Helper functions to convert API client enums to our local enums
function convertFeatureStatus(apiStatus: any): FeatureStatus {
  // Handle both numeric and string values from the API
  if (typeof apiStatus === 'number') {
    switch (apiStatus) {
      case 0: return FeatureStatus.Active;
      case 1: return FeatureStatus.Inactive;
      case 2: return FeatureStatus.NeedsConfiguration;
      case 3: return FeatureStatus.Disabled;
      case 4: return FeatureStatus.Unavailable;
      default: return FeatureStatus.Inactive;
    }
  }
  
  // Handle string values
  switch (apiStatus) {
    case 'Active': return FeatureStatus.Active;
    case 'Inactive': return FeatureStatus.Inactive;
    case 'NeedsConfiguration': return FeatureStatus.NeedsConfiguration;
    case 'Disabled': return FeatureStatus.Disabled;
    case 'Unavailable': return FeatureStatus.Unavailable;
    default: return FeatureStatus.Inactive;
  }
}

function convertIntegrationStatus(apiStatus: any): IntegrationStatus {
  // Handle both numeric and string values from the API
  if (typeof apiStatus === 'number') {
    switch (apiStatus) {
      case 0: return IntegrationStatus.Connected;
      case 1: return IntegrationStatus.Disconnected;
      case 2: return IntegrationStatus.NeedsConfiguration;
      case 3: return IntegrationStatus.Error;
      case 4: return IntegrationStatus.Disabled;
      default: return IntegrationStatus.Disconnected;
    }
  }
  
  // Handle string values
  switch (apiStatus) {
    case 'Connected': return IntegrationStatus.Connected;
    case 'Disconnected': return IntegrationStatus.Disconnected;
    case 'NeedsConfiguration': return IntegrationStatus.NeedsConfiguration;
    case 'Error': return IntegrationStatus.Error;
    case 'Disabled': return IntegrationStatus.Disabled;
    default: return IntegrationStatus.Disconnected;
  }
}

// Re-export the types for easier use in components
export { 
  IntegrationsAndFeaturesDto, 
  FeatureDto, 
  IntegrationDto, 
  SystemHealthDto,
  IntegrationHealthCheckDto,
  ConfigurationGuideDto,
  FeatureUsageDto,
  ConfigurationStepDto
};

export interface ApiResponse<T> {
  data: T;
  message: string;
  success: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class IntegrationsFeaturesService {
  constructor(private apiClient: TechTickerApiClient) { }

  /**
   * Gets the complete overview of all features and integrations
   */
  getIntegrationsAndFeatures(): Observable<ApiResponse<IntegrationsAndFeaturesDto>> {
    return this.apiClient.getIntegrationsAndFeatures().pipe(
      map(response => {
        const data = response.data!;
        
        // Convert enum values for features
        if (data.features) {
          data.features.forEach(feature => {
            if (feature.status !== undefined) {
              feature.status = convertFeatureStatus(feature.status) as any;
            }
          });
        }
        
        // Convert enum values for integrations
        if (data.integrations) {
          data.integrations.forEach(integration => {
            if (integration.status !== undefined) {
              integration.status = convertIntegrationStatus(integration.status) as any;
            }
          });
        }
        
        return {
          data: data,
          message: response.message || 'System overview retrieved successfully',
          success: response.success ?? true
        };
      })
    );
  }

  /**
   * Gets all available features with their current status
   */
  getFeatures(): Observable<ApiResponse<FeatureDto[]>> {
    return this.apiClient.getFeatures().pipe(
      map(response => ({
        data: response.data!,
        message: response.message || 'Features retrieved successfully',
        success: response.success ?? true
      }))
    );
  }

  /**
   * Gets all integrations with their current status
   */
  getIntegrations(): Observable<ApiResponse<IntegrationDto[]>> {
    return this.apiClient.getIntegrations().pipe(
      map(response => ({
        data: response.data!,
        message: response.message || 'Integrations retrieved successfully',
        success: response.success ?? true
      }))
    );
  }

  /**
   * Gets system health overview
   */
  getSystemHealth(): Observable<ApiResponse<SystemHealthDto>> {
    return this.apiClient.getSystemHealth2().pipe(
      map(response => ({
        data: response.data!,
        message: response.message || 'System health retrieved successfully',
        success: response.success ?? true
      }))
    );
  }

  /**
   * Performs health check on a specific integration
   */
  checkIntegrationHealth(integrationId: string): Observable<ApiResponse<IntegrationHealthCheckDto>> {
    return this.apiClient.checkIntegrationHealth(integrationId).pipe(
      map(response => ({
        data: response.data!,
        message: response.message || 'Health check completed',
        success: response.success ?? true
      }))
    );
  }

  /**
   * Gets configuration guide for a specific feature or integration
   */
  getConfigurationGuide(id: string): Observable<ApiResponse<ConfigurationGuideDto>> {
    return this.apiClient.getConfigurationGuide(id).pipe(
      map(response => ({
        data: response.data!,
        message: response.message || 'Configuration guide retrieved successfully',
        success: response.success ?? true
      }))
    );
  }

  /**
   * Records feature usage for analytics
   */
  recordFeatureUsage(featureId: string): Observable<ApiResponse<boolean>> {
    return this.apiClient.recordFeatureUsage(featureId).pipe(
      map(response => ({
        data: response.data!,
        message: response.message || 'Feature usage recorded',
        success: response.success ?? true
      }))
    );
  }

  /**
   * Gets feature usage statistics
   */
  getFeatureUsage(featureId: string): Observable<ApiResponse<FeatureUsageDto>> {
    return this.apiClient.getFeatureUsage(featureId).pipe(
      map(response => ({
        data: response.data!,
        message: response.message || 'Feature usage retrieved successfully',
        success: response.success ?? true
      }))
    );
  }

  /**
   * Refreshes all integration health checks
   */
  refreshIntegrationHealth(): Observable<ApiResponse<boolean>> {
    return this.apiClient.refreshIntegrationHealth().pipe(
      map(response => ({
        data: response.data!,
        message: response.message || 'Integration health refreshed successfully',
        success: response.success ?? true
      }))
    );
  }
} 