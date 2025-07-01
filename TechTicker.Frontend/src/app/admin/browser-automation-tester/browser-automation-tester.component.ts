import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatBadgeModule } from '@angular/material/badge';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { TechTickerApiClient, BrowserAutomationTestRequestDto, SaveTestResultRequestDto, BrowserAutomationProfileDto, BrowserTestOptionsDto, BrowserAutomationActionDto } from '../../shared/api/api-client';
import { BrowserAutomationTestHubService } from './services/browser-automation-test-hub.service';
import { AdvancedTestConfigDialogComponent } from './components/advanced-test-config-dialog.component';
import { TestResultsHistoryComponent } from './components/test-results-history.component';
declare var jsPDF: any;

@Component({
  selector: 'app-browser-automation-tester',
  templateUrl: './browser-automation-tester.component.html',
  styleUrls: ['./browser-automation-tester.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatToolbarModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatTabsModule,
    MatSnackBarModule,
    MatDialogModule,
    MatSlideToggleModule,
    MatSelectModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    MatMenuModule,
    MatTooltipModule,
    MatDividerModule,
    MatChipsModule,
    MatExpansionModule,
    MatBadgeModule,
    TestResultsHistoryComponent
  ]
})
export class BrowserAutomationTesterComponent implements OnInit, OnDestroy {
  // Expose Math to template
  Math = Math;
  
  // Form and state
  testerForm: FormGroup;
  isTestRunning = false;
  currentSessionId: string | null = null;
  testProgress = 0;
  currentAction = '';
  loading = false;

  // UI state
  browserFullscreen = false;
  autoScroll = true;
  selectedTab = 0;
  panelSizes = {
    profilePanel: 25,
    browserPanel: 50,
    infoPanel: 25
  };
  showAdvancedSettings = false;

  // Real-time data
  browserState: any = null;
  logs: any[] = [];
  metrics: any = null;
  testResults: any = null;
  currentScreenshot: string | null = null;
  networkRequests: any[] = [];
  consoleMessages: any[] = [];
  savedResultsCount = 0;

  // Enhanced test options with Phase 2 features
  testOptions = {
    // Browser configuration
    recordVideo: false,
    captureScreenshots: true,
    slowMotion: 0,
    headless: false,
    browserEngine: 'chromium', // Enhanced browser selection
    
    // Logging options
    enableNetworkLogging: true,
    enableConsoleLogging: true,
    enablePerformanceLogging: true,
    
    // Enhanced viewport settings
    viewportWidth: 1920,
    viewportHeight: 1080,
    deviceEmulation: 'desktop',
    userAgent: '',
    
    // Enhanced timeout settings
    testTimeoutMs: 60000,
    actionTimeoutMs: 30000,
    navigationTimeoutMs: 30000,
    
    // Phase 2: Advanced features
    enableVideoRecording: false,
    videoQuality: 'medium', // low, medium, high
    screenshotFormat: 'png', // png, jpeg
    screenshotQuality: 80, // for jpeg
    enableHAR: false, // HTTP Archive recording
    enableTrace: false, // Performance trace
    proxySettings: {
      enabled: false,
      server: '',
      username: '',
      password: ''
    },
    customHeaders: {} as { [key: string]: string }
  };

  // Enhanced device presets with Phase 2 features
  devicePresets = [
    { value: 'desktop', label: 'Desktop (1920x1080)', width: 1920, height: 1080, userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36' },
    { value: 'laptop', label: 'Laptop (1366x768)', width: 1366, height: 768, userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36' },
    { value: 'tablet', label: 'Tablet (768x1024)', width: 768, height: 1024, userAgent: 'Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1' },
    { value: 'mobile', label: 'Mobile (375x667)', width: 375, height: 667, userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1' },
    { value: 'mobile-large', label: 'Mobile Large (414x896)', width: 414, height: 896, userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1' },
    { value: 'custom', label: 'Custom', width: 1920, height: 1080, userAgent: '' }
  ];

  // Browser engine options for Phase 2
  browserEngines = [
    { value: 'chromium', label: 'Chromium (Recommended)', description: 'Google Chrome/Microsoft Edge compatible' },
    { value: 'firefox', label: 'Firefox', description: 'Mozilla Firefox compatible' },
    { value: 'webkit', label: 'WebKit', description: 'Safari compatible' }
  ];

  constructor(
    private fb: FormBuilder,
    private apiClient: TechTickerApiClient,
    private hubService: BrowserAutomationTestHubService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {
    this.testerForm = this.fb.group({
      testUrl: ['https://example.com', [Validators.required, Validators.pattern(/^https?:\/\/.+/)]],
      sessionName: [''],
      saveResults: [false]
    });
  }

  ngOnInit(): void {
    this.setupSignalRConnection();
    this.loadSavedResultsCount();
  }

  ngOnDestroy(): void {
    this.hubService.disconnect();
    if (this.currentSessionId) {
      this.stopTest();
    }
  }

  // Add computed properties for template expressions
  get errorLogCount(): number {
    return this.logs.filter(l => l.level === 'error').length;
  }

  get hasErrors(): boolean {
    return this.errorLogCount > 0;
  }

  get hasTestResults(): boolean {
    return !!this.testResults;
  }

  get isTestSuccessful(): boolean {
    return this.testResults?.success || false;
  }

  get isTestFailed(): boolean {
    return this.hasTestResults && !this.isTestSuccessful;
  }

  get testResultIcon(): string {
    return this.isTestSuccessful ? 'check_circle' : 'error';
  }

  get testResultTitle(): string {
    return this.isTestSuccessful ? 'Test Completed Successfully' : 'Test Failed';
  }

  get testDuration(): number {
    return this.testResults?.duration || 0;
  }

  get testActionsExecuted(): number {
    return this.testResults?.actionsExecuted || 0;
  }

  get testErrorCount(): number {
    return this.testResults?.errors?.length || 0;
  }

  get hasTestErrors(): boolean {
    return this.testErrorCount > 0;
  }

  // Helper method to prevent event propagation
  stopPropagation(event: Event): void {
    event.stopPropagation();
  }

  private setupSignalRConnection(): void {
    // Subscribe to real-time updates
    this.hubService.browserStateUpdate$.subscribe((update) => {
      this.browserState = update;
      this.testProgress = update.progress || 0;
      this.currentAction = update.currentAction || '';
      if (update.screenshot) {
        this.currentScreenshot = update.screenshot;
      }
    });

    this.hubService.logEntry$.subscribe((logEntry) => {
      this.logs.push(logEntry);
      
      // Phase 2: Enhanced logging categorization
      if (logEntry.category === 'network') {
        this.networkRequests.push({
          ...logEntry,
          timestamp: new Date(logEntry.timestamp)
        });
      } else if (logEntry.category === 'console') {
        this.consoleMessages.push({
          ...logEntry,
          timestamp: new Date(logEntry.timestamp)
        });
      }
      
      if (this.autoScroll) {
        setTimeout(() => this.scrollLogsToBottom(), 100);
      }
    });

    this.hubService.performanceMetrics$.subscribe((metrics) => {
      this.metrics = metrics;
    });

    this.hubService.testCompleted$.subscribe((result) => {
      this.testResults = result;
      this.isTestRunning = false;
      this.currentSessionId = null;
      this.showSuccessMessage('Test completed successfully!');
    });

    this.hubService.error$.subscribe((error) => {
      this.showErrorMessage(`Test error: ${error.message}`);
      this.isTestRunning = false;
      this.currentSessionId = null;
    });
  }

  // Phase 2: Advanced configuration dialog
  openAdvancedSettings(): void {
    const dialogRef = this.dialog.open(AdvancedTestConfigDialogComponent, {
      width: '800px',
      maxHeight: '80vh',
      data: { testOptions: { ...this.testOptions } }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.testOptions = { ...result };
        this.showSuccessMessage('Advanced settings updated');
      }
    });
  }

  // Phase 2: Enhanced device management
  onDeviceChange(): void {
    const selectedDevice = this.devicePresets.find(d => d.value === this.testOptions.deviceEmulation);
    if (selectedDevice && selectedDevice.value !== 'custom') {
      this.testOptions.viewportWidth = selectedDevice.width;
      this.testOptions.viewportHeight = selectedDevice.height;
      if (selectedDevice.userAgent) {
        this.testOptions.userAgent = selectedDevice.userAgent;
      }
    }
  }

  // Phase 2: Panel resizing
  onPanelResize(event: any, panel: string): void {
    // Implementation for resizable panels
    // This would typically use a library like Angular CDK Layout
    console.log(`Panel ${panel} resized:`, event);
  }

  // Phase 2: Enhanced fullscreen toggle
  toggleFullscreen(): void {
    this.browserFullscreen = !this.browserFullscreen;
    if (this.browserFullscreen) {
      // Hide other panels when in fullscreen
      this.panelSizes = { profilePanel: 0, browserPanel: 100, infoPanel: 0 };
    } else {
      // Restore panel sizes
      this.panelSizes = { profilePanel: 25, browserPanel: 50, infoPanel: 25 };
    }
  }

  // Phase 2: Enhanced screenshot capture with options
  async captureScreenshot(): Promise<void> {
    if (!this.currentSessionId) {
      this.showErrorMessage('No active test session');
      return;
    }

    try {
      const result = await this.apiClient.getTestSessionScreenshot(this.currentSessionId).toPromise();
      if (result?.data) {
        // Create download link for screenshot
        const link = document.createElement('a');
        link.href = `data:image/${this.testOptions.screenshotFormat};base64,${result.data}`;
        link.download = `screenshot_${Date.now()}.${this.testOptions.screenshotFormat}`;
        link.click();
        
        this.showSuccessMessage('Screenshot captured successfully!');
      }
    } catch (error: any) {
      console.error('Error capturing screenshot:', error);
      this.showErrorMessage(`Failed to capture screenshot: ${error.message}`);
    }
  }

  // Phase 2: Enhanced export capabilities
  async exportTestData(format: 'json' | 'csv' | 'pdf' = 'json'): Promise<void> {
    try {
      const exportData = {
        session: {
          id: this.currentSessionId,
          testUrl: this.testerForm.value.testUrl,
          startTime: new Date().toISOString(),
          options: this.testOptions
        },
        logs: this.logs,
        networkRequests: this.networkRequests,
        consoleMessages: this.consoleMessages,
        metrics: this.metrics,
        results: this.testResults
      };

      let content: string;
      let mimeType: string;
      let extension: string;

      switch (format) {
        case 'json':
          content = JSON.stringify(exportData, null, 2);
          mimeType = 'application/json';
          extension = 'json';
          break;
        case 'csv':
          content = this.convertToCSV(exportData);
          mimeType = 'text/csv';
          extension = 'csv';
          break;
        case 'pdf':
          await this.convertToPDF(exportData);
          // PDF is handled differently, no blob creation needed
          this.showSuccessMessage(`Test data exported as PDF`);
          return;
        default:
          throw new Error('Unsupported export format');
      }

      const blob = new Blob([content], { type: mimeType });
      const link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = `test-data-${Date.now()}.${extension}`;
      link.click();

      this.showSuccessMessage(`Test data exported as ${format.toUpperCase()}`);
    } catch (error: any) {
      console.error('Error exporting test data:', error);
      this.showErrorMessage(`Failed to export test data: ${error.message}`);
    }
  }

  private convertToCSV(data: any): string {
    // Simple CSV conversion for logs
    const headers = ['Timestamp', 'Level', 'Category', 'Message'];
    const rows = data.logs.map((log: any) => [
      new Date(log.timestamp).toISOString(),
      log.level,
      log.category,
      log.message.replace(/"/g, '""') // Escape quotes
    ]);

    return [
      headers.join(','),
      ...rows.map((row: any[]) => row.map(cell => `"${cell}"`).join(','))
    ].join('\n');
  }

  private async convertToPDF(data: any): Promise<void> {
    try {
      // Create a new PDF document using dynamic import to avoid build issues
      const jsPDFModule = await import('jspdf');
      const jsPDFClass = (jsPDFModule as any).jsPDF || jsPDFModule.default;
      const doc = new jsPDFClass();

    // Add title
    doc.setFontSize(18);
    doc.text('Browser Automation Test Report', 20, 20);

    // Add test information
    doc.setFontSize(12);
    let yPosition = 40;
    
    if (this.testResults) {
      doc.text(`Test URL: ${this.testerForm.value.testUrl}`, 20, yPosition);
      yPosition += 10;
      doc.text(`Test Status: ${this.isTestSuccessful ? 'Success' : 'Failed'}`, 20, yPosition);
      yPosition += 10;
      doc.text(`Duration: ${this.formatDuration(this.testDuration)}`, 20, yPosition);
      yPosition += 10;
      doc.text(`Actions Executed: ${this.testActionsExecuted}`, 20, yPosition);
      yPosition += 10;
      doc.text(`Error Count: ${this.testErrorCount}`, 20, yPosition);
      yPosition += 20;
    }

    // Add logs section
    doc.setFontSize(14);
    doc.text('Test Logs', 20, yPosition);
    yPosition += 15;

    // Prepare logs data for table
    const logData = data.logs.slice(0, 50).map((log: any) => [
      new Date(log.timestamp).toLocaleTimeString(),
      log.level.toUpperCase(),
      log.category || 'General',
      log.message.substring(0, 60) + (log.message.length > 60 ? '...' : '')
    ]);

    // Add table using autoTable plugin
    (doc as any).autoTable({
      startY: yPosition,
      head: [['Time', 'Level', 'Category', 'Message']],
      body: logData,
      theme: 'striped',
      styles: {
        fontSize: 8,
        cellPadding: 2
      },
      headStyles: {
        fillColor: [41, 128, 185],
        textColor: 255
      },
      alternateRowStyles: {
        fillColor: [245, 245, 245]
      }
    });

    // Add footer
    const pageCount = doc.getNumberOfPages();
    for (let i = 1; i <= pageCount; i++) {
      doc.setPage(i);
      doc.setFontSize(10);
      doc.text(`Page ${i} of ${pageCount}`, doc.internal.pageSize.getWidth() - 40, doc.internal.pageSize.getHeight() - 10);
      doc.text(`Generated: ${new Date().toLocaleString()}`, 20, doc.internal.pageSize.getHeight() - 10);
    }

      // Save the PDF
      const fileName = `test-report-${Date.now()}.pdf`;
      doc.save(fileName);
    } catch (error) {
      console.error('Error generating PDF:', error);
      throw new Error('Failed to generate PDF report');
    }
  }

  async startTest(): Promise<void> {
    if (!this.testerForm.valid) {
      this.showErrorMessage('Please enter a valid test URL');
      return;
    }

    try {
      this.isTestRunning = true;
      this.testProgress = 0;
      this.currentAction = 'Starting test...';
      this.logs = [];
      this.testResults = null;
      this.currentScreenshot = null;

      // Create basic profile for MVP (just navigate to URL) using DTOs
      const basicAction = new BrowserAutomationActionDto({
        actionType: 'navigate',
        value: this.testerForm.value.testUrl,
        delayMs: 2000
      });
      const basicProfile = new BrowserAutomationProfileDto({
        actions: [basicAction],
        timeoutSeconds: 30
      });

      // Prepare test options using DTO constructor
      const testOptions = new BrowserTestOptionsDto({ ...this.testOptions });

      // Prepare test request using DTO constructor
      const testRequest = new BrowserAutomationTestRequestDto({
        testUrl: this.testerForm.value.testUrl,
        profile: basicProfile,
        options: testOptions,
        saveResults: this.testerForm.value.saveResults,
        sessionName: this.testerForm.value.sessionName || undefined
      });

      // Start test session
      const response = await this.apiClient.startTestSession(testRequest).toPromise();
      
      if (response?.data) {
        this.currentSessionId = response.data.id!;
        await this.hubService.connect();
        await this.hubService.joinTestSession(this.currentSessionId);
        this.showSuccessMessage('Test started successfully!');
      }
    } catch (error: any) {
      console.error('Error starting test:', error);
      this.showErrorMessage(`Failed to start test: ${error.message}`);
      this.isTestRunning = false;
    }
  }

  async stopTest(): Promise<void> {
    if (!this.currentSessionId) return;

    try {
      await this.apiClient.stopTestSession(this.currentSessionId).toPromise();
      await this.hubService.leaveTestSession(this.currentSessionId);
      this.currentSessionId = null;
      this.isTestRunning = false;
      this.showSuccessMessage('Test stopped successfully!');
    } catch (error: any) {
      console.error('Error stopping test:', error);
      this.showErrorMessage(`Failed to stop test: ${error.message}`);
    }
  }

  clearLogs(): void {
    this.logs = [];
  }

  async exportLogs(): Promise<void> {
    try {
      const logsData = JSON.stringify(this.logs, null, 2);
      const blob = new Blob([logsData], { type: 'application/json' });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `browser-automation-logs-${new Date().toISOString()}.json`;
      link.click();
      window.URL.revokeObjectURL(url);
      this.showSuccessMessage('Logs exported successfully!');
    } catch (error: any) {
      console.error('Error exporting logs:', error);
      this.showErrorMessage(`Failed to export logs: ${error.message}`);
    }
  }

  async saveResults(): Promise<void> {
    if (!this.currentSessionId || !this.testResults) return;

    try {
      const request = new SaveTestResultRequestDto({
        name: `Test Session ${new Date().toISOString()}`,
        description: `Browser automation test for ${this.testerForm.value.testUrl}`,
        tags: ['browser-automation', 'test']
      });

      const response = await this.apiClient.saveTestResults(this.currentSessionId, request).toPromise();
      if (response?.data) {
        this.showSuccessMessage(`Test results saved with ID: ${response.data}`);
      }
    } catch (error: any) {
      console.error('Error saving test results:', error);
      this.showErrorMessage(`Failed to save test results: ${error.message}`);
    }
  }

  private scrollLogsToBottom(): void {
    try {
      const logsContainer = document.querySelector('.logs-content');
      if (logsContainer) {
        logsContainer.scrollTop = logsContainer.scrollHeight;
      }
    } catch (err) {
      console.error('Error scrolling logs:', err);
    }
  }

  private showSuccessMessage(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: ['success-snackbar']
    });
  }

  private showErrorMessage(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }

  getLogLevelIcon(level: string): string {
    switch (level?.toLowerCase()) {
      case 'error': return 'error';
      case 'warn': return 'warning';
      case 'info': return 'info';
      case 'debug': return 'bug_report';
      default: return 'info';
    }
  }

  getLogLevelClass(level: string): string {
    return `log-level-${level?.toLowerCase() || 'info'}`;
  }

  getActionIcon(actionType: string): string {
    switch (actionType?.toLowerCase()) {
      case 'click': return 'touch_app';
      case 'type': return 'keyboard';
      case 'navigate': return 'navigation';
      case 'wait': return 'schedule';
      case 'scroll': return 'unfold_more';
      default: return 'play_arrow';
    }
  }

  formatTimestamp(timestamp: string | Date): string {
    try {
      const date = new Date(timestamp);
      return date.toLocaleTimeString('en-US', { 
        hour12: false, 
        hour: '2-digit', 
        minute: '2-digit', 
        second: '2-digit',
        fractionalSecondDigits: 3
      });
    } catch {
      return String(timestamp);
    }
  }

  formatDuration(durationMs: number): string {
    if (durationMs < 1000) {
      return `${durationMs}ms`;
    } else if (durationMs < 60000) {
      return `${(durationMs / 1000).toFixed(1)}s`;
    } else {
      const minutes = Math.floor(durationMs / 60000);
      const seconds = Math.floor((durationMs % 60000) / 1000);
      return `${minutes}m ${seconds}s`;
    }
  }

  trackByLogIndex(index: number, item: any): number {
    return index;
  }

  async loadSavedResultsCount(): Promise<void> {
    try {
      // For now, set a default count. This would normally call the API
      // const response = await this.apiClient.getTestStatistics().toPromise();
      // this.savedResultsCount = response?.data?.totalTests || 0;
      this.savedResultsCount = 0;
    } catch (error) {
      console.error('Error loading saved results count:', error);
      this.savedResultsCount = 0;
    }
  }

  shareResults(): void {
    if (!this.testResults) {
      this.showErrorMessage('No test results to share');
      return;
    }

    // Create a shareable link or copy test results to clipboard
    const shareData = {
      title: 'Browser Automation Test Results',
      text: `Test completed ${this.isTestSuccessful ? 'successfully' : 'with errors'} in ${this.formatDuration(this.testDuration)}`,
      url: window.location.href
    };

    if (navigator.share) {
      navigator.share(shareData).catch(() => {
        // Fallback to clipboard
        this.copyToClipboard(JSON.stringify(this.testResults, null, 2));
      });
    } else {
      // Fallback to clipboard
      this.copyToClipboard(JSON.stringify(this.testResults, null, 2));
    }
  }

  private copyToClipboard(text: string): void {
    navigator.clipboard.writeText(text).then(() => {
      this.showSuccessMessage('Test results copied to clipboard');
    }).catch(() => {
      this.showErrorMessage('Failed to copy to clipboard');
    });
  }
} 