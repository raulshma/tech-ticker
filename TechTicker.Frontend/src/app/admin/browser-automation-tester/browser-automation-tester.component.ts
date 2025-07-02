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
import { ActionTemplatesDialogComponent, BrowserAutomationAction } from './components/action-templates-dialog.component';
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

  // Action templates and current actions
  currentActions: BrowserAutomationAction[] = [];
  actionTemplatesCount = 0;

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
      testUrl: ['', [Validators.required, Validators.pattern(/^https?:\/\/.{3,}$/)]],
      sessionName: [''],
      saveResults: [false]
    });
  }

  ngOnInit(): void {
    this.setupSignalRConnection();
    this.loadSavedResultsCount();
    this.loadActionTemplatesCount();
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

      const testUrl = this.testerForm.value.testUrl;

      // Create profile with current actions or basic navigation
      let actions: BrowserAutomationActionDto[] = [];
      
      if (this.currentActions.length > 0) {
        // Convert current actions to DTOs and ensure navigate actions use the correct URL
        actions = this.currentActions.map(action => {
          const actionDto = new BrowserAutomationActionDto({
            actionType: action.actionType,
            selector: action.selector,
            repeat: action.repeat,
            delayMs: action.delayMs,
            value: action.value
          });

          // If this is a navigate action and no specific URL is set, use the test URL
          if (action.actionType === 'navigate' && (!action.value || action.value === 'https://example.com' || action.value.trim() === '')) {
            actionDto.value = testUrl;
          }

          return actionDto;
        });

        // If no navigate action exists at the beginning, prepend one
        const hasNavigateAction = actions.some(action => action.actionType === 'navigate');
        if (!hasNavigateAction) {
          actions.unshift(new BrowserAutomationActionDto({
            actionType: 'navigate',
            value: testUrl,
            delayMs: 2000
          }));
        }
      } else {
        // Fallback to basic navigation action when no current actions exist
        actions = [new BrowserAutomationActionDto({
          actionType: 'navigate',
          value: testUrl,
          delayMs: 2000
        })];
      }

      const profile = new BrowserAutomationProfileDto({
        actions: actions,
        timeoutSeconds: 30
      });

      // Prepare test options using DTO constructor
      const testOptions = new BrowserTestOptionsDto({ ...this.testOptions });

      // Prepare test request using DTO constructor
      const testRequest = new BrowserAutomationTestRequestDto({
        testUrl: this.testerForm.value.testUrl,
        profile: profile,
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
        // Refresh the saved results count to update the badge
        await this.loadSavedResultsCount();
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
      // Navigation Actions
      case 'navigate':
      case 'goto':
      case 'url': return 'navigation';
      case 'reload':
      case 'refresh': return 'refresh';
      case 'goback': return 'arrow_back';
      case 'goforward': return 'arrow_forward';
      
      // Clicking Actions
      case 'click': return 'touch_app';
      case 'doubleclick':
      case 'rightclick': return 'mouse';
      
      // Input Actions
      case 'type': return 'keyboard';
      case 'clear': return 'clear';
      case 'setvalue': return 'input';
      case 'press': return 'keyboard';
      case 'upload': return 'upload_file';
      
      // Focus Actions
      case 'focus': return 'center_focus_strong';
      case 'blur': return 'blur_on';
      case 'hover': return 'mouse';
      
      // Wait Actions
      case 'wait':
      case 'waitfortimeout': return 'timer';
      case 'waitforselector': return 'schedule';
      case 'waitfornavigation': return 'hourglass_empty';
      case 'waitforloadstate': return 'hourglass_full';
      
      // Scroll Actions
      case 'scroll': return 'unfold_more';
      
      // Selection Actions
      case 'selectoption': return 'arrow_drop_down';
      
      // Media Actions
      case 'screenshot': return 'camera_alt';
      
      // JavaScript Actions
      case 'evaluate': return 'code';
      
      // Drag & Drop Actions
      case 'drag': return 'drag_indicator';
      
      // Window Management
      case 'maximize': return 'fullscreen';
      case 'minimize': return 'fullscreen_exit';
      case 'fullscreen': return 'fullscreen';
      case 'newtab':
      case 'newpage': return 'tab';
      case 'closetab':
      case 'closepage': return 'close';
      case 'switchwindow':
      case 'switchtab': return 'swap_horiz';
      
      // Frame Actions
      case 'switchframe':
      case 'switchiframe': return 'web_asset';
      
      // Alert Actions
      case 'alert': return 'warning';
      case 'acceptalert': return 'check_circle';
      case 'dismissalert': return 'cancel';
      
      // Cookie Actions
      case 'getcookies':
      case 'setcookies': return 'cookie';
      case 'deletecookies': return 'delete_sweep';
      
      // Style & Script Injection
      case 'addstylesheet': return 'style';
      case 'addscript': return 'code';
      
      // Device Emulation
      case 'emulatedevice': return 'smartphone';
      
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
      const response = await this.apiClient.getTestStatistics().toPromise();
      if (response?.success && response.data) {
        this.savedResultsCount = response.data.totalTests || 0;
      } else {
        this.savedResultsCount = 0;
      }
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

  // Action Templates Methods
  async loadActionTemplatesCount(): Promise<void> {
    try {
      const stored = localStorage.getItem('browser-automation-action-templates');
      if (stored) {
        const templates = JSON.parse(stored);
        this.actionTemplatesCount = templates.length;
      } else {
        // Create default templates if none exist
        this.createDefaultTemplates();
      }
    } catch (error) {
      console.error('Error loading action templates count:', error);
      this.actionTemplatesCount = 0;
    }
  }

  private createDefaultTemplates(): void {
    const defaultTemplates = [
      {
        id: 'basic-navigation',
        name: 'Basic Navigation',
        description: 'Simple navigation to a URL with wait (uses current test URL)',
        category: 'navigation',
        actions: [
          { actionType: 'navigate', value: '', delayMs: 2000 }, // Empty value will use test URL
          { actionType: 'waitForLoadState', value: 'networkidle', delayMs: 1000 },
          { actionType: 'screenshot', delayMs: 500 }
        ],
        createdAt: new Date(),
        usageCount: 0
      },
      {
        id: 'form-filling',
        name: 'Form Filling',
        description: 'Fill out a form with text input and clear/focus actions',
        category: 'form-filling',
        actions: [
          { actionType: 'waitForSelector', selector: 'input[name="username"]', delayMs: 1000 },
          { actionType: 'focus', selector: 'input[name="username"]', delayMs: 300 },
          { actionType: 'clear', selector: 'input[name="username"]', delayMs: 300 },
          { actionType: 'type', selector: 'input[name="username"]', value: 'testuser', delayMs: 500 },
          { actionType: 'type', selector: 'input[name="password"]', value: 'testpass', delayMs: 500 },
          { actionType: 'click', selector: 'button[type="submit"]', delayMs: 1000 }
        ],
        createdAt: new Date(),
        usageCount: 0
      },
      {
        id: 'screenshot-sequence',
        name: 'Screenshot Sequence',
        description: 'Navigate and take screenshots with scrolling (uses current test URL)',
        category: 'screenshots',
        actions: [
          { actionType: 'navigate', value: '', delayMs: 2000 }, // Empty value will use test URL
          { actionType: 'waitForLoadState', value: 'networkidle', delayMs: 1000 },
          { actionType: 'screenshot', delayMs: 1000 },
          { actionType: 'scroll', delayMs: 1000 },
          { actionType: 'screenshot', delayMs: 1000 },
          { actionType: 'scroll', delayMs: 1000 },
          { actionType: 'screenshot', delayMs: 1000 }
        ],
        createdAt: new Date(),
        usageCount: 0
      },
      {
        id: 'ecommerce-browse',
        name: 'E-commerce Browse',
        description: 'Browse an e-commerce site with hover and double-click (uses current test URL)',
        category: 'ecommerce',
        actions: [
          { actionType: 'navigate', value: '', delayMs: 2000 }, // Empty value will use test URL
          { actionType: 'waitForSelector', selector: '.product-grid', delayMs: 1000 },
          { actionType: 'hover', selector: '.product-item:first-child', delayMs: 800 },
          { actionType: 'click', selector: '.product-item:first-child', delayMs: 1000 },
          { actionType: 'waitForNavigation', delayMs: 2000 },
          { actionType: 'waitForSelector', selector: '.product-details', delayMs: 1000 },
          { actionType: 'screenshot', delayMs: 1000 }
        ],
        createdAt: new Date(),
        usageCount: 0
      },
      {
        id: 'advanced-interactions',
        name: 'Advanced Interactions',
        description: 'Demonstrate keyboard, alerts, and window management actions',
        category: 'testing',
        actions: [
          { actionType: 'navigate', value: '', delayMs: 2000 }, // Empty value will use test URL
          { actionType: 'press', value: 'F11', delayMs: 1000 }, // Fullscreen
          { actionType: 'wait', value: '2000', delayMs: 0 },
          { actionType: 'press', value: 'Escape', delayMs: 1000 }, // Exit fullscreen
          { actionType: 'rightclick', selector: 'body', delayMs: 1000 },
          { actionType: 'press', value: 'Escape', delayMs: 500 }, // Close context menu
          { actionType: 'screenshot', delayMs: 1000 }
        ],
        createdAt: new Date(),
        usageCount: 0
      },
      {
        id: 'multi-tab-workflow',
        name: 'Multi-Tab Workflow',
        description: 'Demonstrate tab management and switching',
        category: 'navigation',
        actions: [
          { actionType: 'navigate', value: '', delayMs: 2000 }, // Empty value will use test URL
          { actionType: 'screenshot', delayMs: 1000 },
          { actionType: 'newtab', value: 'https://github.com', delayMs: 2000 },
          { actionType: 'waitForLoadState', value: 'networkidle', delayMs: 2000 },
          { actionType: 'screenshot', delayMs: 1000 },
          { actionType: 'switchtab', value: '0', delayMs: 1000 }, // Switch back to first tab
          { actionType: 'screenshot', delayMs: 1000 }
        ],
        createdAt: new Date(),
        usageCount: 0
      }
    ];

    localStorage.setItem('browser-automation-action-templates', JSON.stringify(defaultTemplates));
    this.actionTemplatesCount = defaultTemplates.length;
  }

  openActionTemplates(): void {
    const dialogRef = this.dialog.open(ActionTemplatesDialogComponent, {
      width: '90vw',
      maxWidth: '1200px',
      height: '80vh',
      maxHeight: '800px',
      data: { 
        currentActions: this.currentActions,
        testUrl: this.testerForm.value.testUrl 
      }
    });

    dialogRef.afterClosed().subscribe((result: any) => {
      if (result?.action === 'use' && result.actions) {
        // Process actions to replace empty navigate URLs with current test URL
        const testUrl = this.testerForm.value.testUrl;
        const processedActions = result.actions.map((action: BrowserAutomationAction) => {
          if (action.actionType === 'navigate' && (!action.value || action.value.trim() === '')) {
            return { ...action, value: testUrl || 'https://example.com' };
          }
          return action;
        });
        
        this.currentActions = processedActions;
        this.showSuccessMessage(`Applied template "${result.template.name}" with ${result.actions.length} actions`);
        this.loadActionTemplatesCount(); // Refresh count
      }
    });
  }

  saveCurrentActionsAsTemplate(): void {
    if (this.currentActions.length === 0) {
      this.snackBar.open('No actions to save. Please add some actions first.', 'Close', { duration: 3000 });
      return;
    }

    const dialogRef = this.dialog.open(ActionTemplatesDialogComponent, {
      width: '90vw',
      maxWidth: '1200px',
      height: '80vh',
      maxHeight: '800px',
      data: { currentActions: this.currentActions }
    });

    dialogRef.afterClosed().subscribe((result: any) => {
      if (result?.action === 'use') {
        this.loadActionTemplatesCount(); // Refresh count
      }
    });
  }

  addAction(action: BrowserAutomationAction): void {
    this.currentActions.push(action);
  }

  removeAction(index: number): void {
    this.currentActions.splice(index, 1);
  }

  clearActions(): void {
    this.currentActions = [];
  }

  addQuickAction(): void {
    // Add a simple navigation action as a quick example
    const testUrl = this.testerForm.value.testUrl;
    const quickAction: BrowserAutomationAction = {
      actionType: 'navigate',
      value: testUrl || 'https://example.com',
      delayMs: 2000
    };
    this.addAction(quickAction);
    this.showSuccessMessage('Quick navigation action added! You can edit it in the action list.');
  }

  addQuickScreenshot(): void {
    const screenshotAction: BrowserAutomationAction = {
      actionType: 'screenshot',
      delayMs: 1000
    };
    this.addAction(screenshotAction);
    this.showSuccessMessage('Screenshot action added!');
  }

  addQuickWait(): void {
    const waitAction: BrowserAutomationAction = {
      actionType: 'wait',
      value: '3000',
      delayMs: 0
    };
    this.addAction(waitAction);
    this.showSuccessMessage('Wait action added (3 seconds)!');
  }

  addQuickScroll(): void {
    const scrollAction: BrowserAutomationAction = {
      actionType: 'scroll',
      delayMs: 1000
    };
    this.addAction(scrollAction);
    this.showSuccessMessage('Scroll action added!');
  }
} 