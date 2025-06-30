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
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { TechTickerApiClient, BrowserAutomationTestRequestDto, SaveTestResultsRequestDto, BrowserAutomationProfileDto, BrowserTestOptionsDto, BrowserAutomationActionDto } from '../../shared/api/api-client';
import { BrowserAutomationTestHubService } from './services/browser-automation-test-hub.service';

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
    MatMenuModule
  ]
})
export class BrowserAutomationTesterComponent implements OnInit, OnDestroy {
  // Form and state
  testerForm: FormGroup;
  isTestRunning = false;
  currentSessionId: string | null = null;
  testProgress = 0;
  currentAction = '';

  // UI state
  browserFullscreen = false;
  autoScroll = true;
  selectedTab = 0;

  // Real-time data
  browserState: any = null;
  logs: any[] = [];
  metrics: any = null;
  testResults: any = null;
  currentScreenshot: string | null = null;

  // Test options
  testOptions = {
    recordVideo: false,
    captureScreenshots: true,
    slowMotion: 0,
    headless: false,
    enableNetworkLogging: true,
    enableConsoleLogging: true,
    enablePerformanceLogging: true,
    viewportWidth: 1920,
    viewportHeight: 1080,
    deviceEmulation: 'desktop',
    testTimeoutMs: 60000,
    actionTimeoutMs: 30000,
    navigationTimeoutMs: 30000
  };

  // Device presets
  devicePresets = [
    { value: 'desktop', label: 'Desktop (1920x1080)', width: 1920, height: 1080 },
    { value: 'laptop', label: 'Laptop (1366x768)', width: 1366, height: 768 },
    { value: 'tablet', label: 'Tablet (768x1024)', width: 768, height: 1024 },
    { value: 'mobile', label: 'Mobile (375x667)', width: 375, height: 667 },
    { value: 'custom', label: 'Custom', width: 1920, height: 1080 }
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
  }

  ngOnDestroy(): void {
    this.hubService.disconnect();
    if (this.currentSessionId) {
      this.stopTest();
    }
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

  toggleFullscreen(): void {
    this.browserFullscreen = !this.browserFullscreen;
  }

  async captureScreenshot(): Promise<void> {
    if (!this.currentSessionId) return;

    try {
      const response = await this.apiClient.getTestSessionScreenshot(this.currentSessionId).toPromise();
      if (response?.data) {
        this.currentScreenshot = response.data;
        this.showSuccessMessage('Screenshot captured!');
      }
    } catch (error: any) {
      console.error('Error capturing screenshot:', error);
      this.showErrorMessage(`Failed to capture screenshot: ${error.message}`);
    }
  }

  onDeviceChange(): void {
    const selectedDevice = this.devicePresets.find(d => d.value === this.testOptions.deviceEmulation);
    if (selectedDevice && selectedDevice.value !== 'custom') {
      this.testOptions.viewportWidth = selectedDevice.width;
      this.testOptions.viewportHeight = selectedDevice.height;
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
      const request = new SaveTestResultsRequestDto({
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
} 