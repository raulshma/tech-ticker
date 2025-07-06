import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { AuthService, CurrentUser } from '../../../../shared/services/auth.service';
import { DashboardService } from '../../services/dashboard.service';
import {
  RealTimeSystemStatusDto,
  BrowserAutomationAnalyticsDto,
  AlertSystemAnalyticsDto,
  ProxyManagementAnalyticsDto,
  ScrapingWorkerAnalyticsDto
} from '../../../../shared/api/api-client';

interface SectionConfiguration {
  id: string;
  name: string;
  icon: string;
  visible: boolean;
  size: 'small' | 'medium' | 'large' | 'full';
}

interface GridConfiguration {
  columns: 'auto' | '1' | '2' | '3' | '4';
  gap: 'small' | 'medium' | 'large';
}

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
  
  // Available sections configuration
  availableSections: SectionConfiguration[] = [
    {
      id: 'realTimeStatus',
      name: 'Real-time Status',
      icon: 'speed',
      visible: true,
      size: 'medium'
    },
    {
      id: 'browserAutomation',
      name: 'Browser Automation',
      icon: 'web',
      visible: true,
      size: 'medium'
    },
    {
      id: 'alertSystem',
      name: 'Alert System',
      icon: 'notifications',
      visible: true,
      size: 'medium'
    },
    {
      id: 'proxyManagement',
      name: 'Proxy Management',
      icon: 'router',
      visible: true,
      size: 'small'
    },
    {
      id: 'scrapingWorker',
      name: 'Scraping Worker',
      icon: 'build',
      visible: true,
      size: 'medium'
    }
  ];

  // Grid configuration
  gridConfig: GridConfiguration = {
    columns: 'auto',
    gap: 'medium'
  };

  // Loading states for each section
  isLoadingRealTimeStatus = false;
  isLoadingBrowserAutomation = false;
  isLoadingAlertSystem = false;
  isLoadingProxyManagement = false;
  isLoadingScrapingWorker = false;

  // Error states for each section
  errorRealTimeStatus: string | null = null;
  errorBrowserAutomation: string | null = null;
  errorAlertSystem: string | null = null;
  errorProxyManagement: string | null = null;
  errorScrapingWorker: string | null = null;

  // Analytics data
  realTimeStatus: RealTimeSystemStatusDto | null = null;
  browserAutomation: BrowserAutomationAnalyticsDto | null = null;
  alertSystem: AlertSystemAnalyticsDto | null = null;
  proxyManagement: ProxyManagementAnalyticsDto | null = null;
  scrapingWorker: ScrapingWorkerAnalyticsDto | null = null;
  
  // Date range for analytics
  dateFrom: Date | null = null;
  dateTo: Date | null = null;

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

    // Load configuration from localStorage
    this.loadConfiguration();

    // Load initial analytics data
    this.loadAllAnalytics();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadConfiguration(): void {
    // Load section configuration
    const savedSections = localStorage.getItem('analytics-dashboard-sections');
    if (savedSections) {
      try {
        const sections = JSON.parse(savedSections);
        this.availableSections = this.availableSections.map(section => {
          const saved = sections.find((s: any) => s.id === section.id);
          return saved ? { ...section, ...saved } : section;
        });
      } catch (error) {
        console.warn('Failed to load section configuration:', error);
      }
    }

    // Load grid configuration
    const savedGrid = localStorage.getItem('analytics-dashboard-grid');
    if (savedGrid) {
      try {
        const grid = JSON.parse(savedGrid);
        this.gridConfig = { ...this.gridConfig, ...grid };
      } catch (error) {
        console.warn('Failed to load grid configuration:', error);
      }
    }
  }

  private saveConfiguration(): void {
    try {
      localStorage.setItem('analytics-dashboard-sections', JSON.stringify(this.availableSections));
      localStorage.setItem('analytics-dashboard-grid', JSON.stringify(this.gridConfig));
    } catch (error) {
      console.warn('Failed to save configuration:', error);
    }
  }

  updateSectionConfig(): void {
    this.saveConfiguration();
    // Reload data for newly visible sections
    this.loadAllAnalytics();
  }

  updateGridConfig(): void {
    this.saveConfiguration();
  }

  getSectionConfig(sectionId: string): SectionConfiguration {
    return this.availableSections.find(s => s.id === sectionId) || this.availableSections[0];
  }

  loadAllAnalytics(): void {
    if (this.getSectionConfig('realTimeStatus').visible) {
      this.loadRealTimeStatus();
    }
    if (this.getSectionConfig('browserAutomation').visible) {
      this.loadBrowserAutomationAnalytics();
    }
    if (this.getSectionConfig('alertSystem').visible) {
      this.loadAlertSystemAnalytics();
    }
    if (this.getSectionConfig('proxyManagement').visible) {
      this.loadProxyManagementAnalytics();
    }
    if (this.getSectionConfig('scrapingWorker').visible) {
      this.loadScrapingWorkerAnalytics();
    }
  }

  loadRealTimeStatus(): void {
    this.isLoadingRealTimeStatus = true;
    this.errorRealTimeStatus = null;

    this.dashboardService.getRealTimeSystemStatus(this.dateFrom, this.dateTo).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (data: RealTimeSystemStatusDto) => {
        this.realTimeStatus = data;
        this.isLoadingRealTimeStatus = false;
      },
      error: (error: any) => {
        console.error('Error loading real-time status:', error);
        this.errorRealTimeStatus = error.message || 'Failed to load real-time status';
        this.isLoadingRealTimeStatus = false;
      }
    });
  }

  loadBrowserAutomationAnalytics(): void {
    this.isLoadingBrowserAutomation = true;
    this.errorBrowserAutomation = null;

    this.dashboardService.getBrowserAutomationAnalytics(this.dateFrom, this.dateTo).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (data: BrowserAutomationAnalyticsDto) => {
        this.browserAutomation = data;
        this.isLoadingBrowserAutomation = false;
      },
      error: (error: any) => {
        console.error('Error loading browser automation data:', error);
        this.errorBrowserAutomation = error.message || 'Failed to load browser automation analytics';
        this.isLoadingBrowserAutomation = false;
      }
    });
  }

  loadAlertSystemAnalytics(): void {
    this.isLoadingAlertSystem = true;
    this.errorAlertSystem = null;

    this.dashboardService.getAlertSystemAnalytics(this.dateFrom, this.dateTo).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (data: AlertSystemAnalyticsDto) => {
        this.alertSystem = data;
        this.isLoadingAlertSystem = false;
      },
      error: (error: any) => {
        console.error('Error loading alert system data:', error);
        this.errorAlertSystem = error.message || 'Failed to load alert system analytics';
        this.isLoadingAlertSystem = false;
      }
    });
  }

  loadProxyManagementAnalytics(): void {
    this.isLoadingProxyManagement = true;
    this.errorProxyManagement = null;

    this.dashboardService.getProxyManagementAnalytics(this.dateFrom, this.dateTo).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (data: ProxyManagementAnalyticsDto) => {
        this.proxyManagement = data;
        this.isLoadingProxyManagement = false;
      },
      error: (error: any) => {
        console.error('Error loading proxy management data:', error);
        this.errorProxyManagement = error.message || 'Failed to load proxy management analytics';
        this.isLoadingProxyManagement = false;
      }
    });
  }

  loadScrapingWorkerAnalytics(): void {
    this.isLoadingScrapingWorker = true;
    this.errorScrapingWorker = null;

    this.dashboardService.getScrapingWorkerAnalytics(this.dateFrom, this.dateTo).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (data: ScrapingWorkerAnalyticsDto) => {
        this.scrapingWorker = data;
        this.isLoadingScrapingWorker = false;
      },
      error: (error: any) => {
        console.error('Error loading scraping worker data:', error);
        this.errorScrapingWorker = error.message || 'Failed to load scraping worker analytics';
        this.isLoadingScrapingWorker = false;
      }
    });
  }

  onDateRangeChange(): void {
    this.loadAllAnalytics();
  }

  // Helper methods to check loading and error states
  get isAnySectionLoading(): boolean {
    return this.isLoadingBrowserAutomation || 
           this.isLoadingAlertSystem || 
           this.isLoadingProxyManagement || 
           this.isLoadingScrapingWorker || 
           this.isLoadingRealTimeStatus;
  }

  get hasAnyError(): boolean {
    return !!(this.errorBrowserAutomation || 
             this.errorAlertSystem || 
             this.errorProxyManagement || 
             this.errorScrapingWorker || 
             this.errorRealTimeStatus);
  }

  // Helper methods for section visibility
  get hasVisibleSections(): boolean {
    return this.availableSections.some(section => section.visible);
  }

  get visibleSectionCount(): number {
    return this.availableSections.filter(section => section.visible).length;
  }

  // TrackBy function for ngFor
  trackByIndex(index: number): number {
    return index;
  }

  // Method to refresh all visible sections
  refreshAllSections(): void {
    this.loadAllAnalytics();
  }

  // Method to reset section configuration to defaults
  resetSectionConfiguration(): void {
    this.availableSections = [
      {
        id: 'realTimeStatus',
        name: 'Real-time Status',
        icon: 'speed',
        visible: true,
        size: 'medium'
      },
      {
        id: 'browserAutomation',
        name: 'Browser Automation',
        icon: 'web',
        visible: true,
        size: 'medium'
      },
      {
        id: 'alertSystem',
        name: 'Alert System',
        icon: 'notifications',
        visible: true,
        size: 'medium'
      },
      {
        id: 'proxyManagement',
        name: 'Proxy Management',
        icon: 'router',
        visible: true,
        size: 'small'
      },
      {
        id: 'scrapingWorker',
        name: 'Scraping Worker',
        icon: 'build',
        visible: true,
        size: 'medium'
      }
    ];

    this.gridConfig = {
      columns: 'auto',
      gap: 'medium'
    };

    this.saveConfiguration();
    this.loadAllAnalytics();
  }

  // Method to get section visibility status
  isSectionVisible(sectionId: string): boolean {
    return this.getSectionConfig(sectionId).visible;
  }

  // Method to get section size
  getSectionSize(sectionId: string): string {
    return this.getSectionConfig(sectionId).size;
  }

  // Method to toggle section visibility
  toggleSectionVisibility(sectionId: string): void {
    const section = this.availableSections.find(s => s.id === sectionId);
    if (section) {
      section.visible = !section.visible;
      this.updateSectionConfig();
    }
  }

  // Method to change section size
  changeSectionSize(sectionId: string, size: 'small' | 'medium' | 'large' | 'full'): void {
    const section = this.availableSections.find(s => s.id === sectionId);
    if (section) {
      section.size = size;
      this.updateSectionConfig();
    }
  }

  // Method to get grid class
  getGridClass(): string {
    return `grid-columns-${this.gridConfig.columns} grid-gap-${this.gridConfig.gap}`;
  }

  // Method to get section class
  getSectionClass(sectionId: string): string {
    const size = this.getSectionSize(sectionId);
    return `section-size-${size}`;
  }
} 