import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';

export interface TestResultDetails {
  id: string;
  name: string;
  description?: string;
  testUrl: string;
  success: boolean;
  executedAt: Date;
  duration: number;
  actionsExecuted: number;
  errorCount: number;
  tags?: string[];
  profileHash: string;
  createdBy: string;
  logs: TestLogEntry[];
  actions: TestActionResult[];
  screenshots: Screenshot[];
  metrics: TestMetrics;
  networkRequests: NetworkRequest[];
  consoleMessages: ConsoleMessage[];
}

export interface TestLogEntry {
  timestamp: Date;
  level: string;
  category: string;
  message: string;
  actionIndex?: number;
  details?: any;
}

export interface TestActionResult {
  index: number;
  actionType: string;
  selector?: string;
  value?: string;
  success: boolean;
  duration: number;
  errorMessage?: string;
  screenshot?: string;
}

export interface Screenshot {
  timestamp: Date;
  actionIndex?: number;
  base64Data: string;
  description?: string;
}

export interface TestMetrics {
  totalLoadTime: number;
  domContentLoaded: number;
  firstContentfulPaint: number;
  largestContentfulPaint: number;
  cumulativeLayoutShift: number;
  memoryUsage?: number;
  networkRequests: number;
}

export interface NetworkRequest {
  url: string;
  method: string;
  status: number;
  responseTime: number;
  size: number;
  timestamp: Date;
}

export interface ConsoleMessage {
  timestamp: Date;
  level: string;
  text: string;
  source?: string;
}

@Component({
  selector: 'app-test-result-details-dialog',
  templateUrl: './test-result-details-dialog.component.html',
  styleUrls: ['./test-result-details-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatCardModule,
    MatChipsModule,
    MatTableModule,
    MatProgressBarModule,
    MatDividerModule,
    MatExpansionModule,
    MatTooltipModule
  ]
})
export class TestResultDetailsDialogComponent implements OnInit {
  // Expose Math to template
  Math = Math;
  
  selectedTabIndex = 0;
  logDisplayedColumns = ['timestamp', 'level', 'category', 'message'];
  actionDisplayedColumns = ['index', 'actionType', 'success', 'duration', 'actions'];
  networkDisplayedColumns = ['method', 'url', 'status', 'responseTime', 'size'];
  consoleDisplayedColumns = ['timestamp', 'level', 'text'];

  constructor(
    public dialogRef: MatDialogRef<TestResultDetailsDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TestResultDetails
  ) {}

  ngOnInit(): void {
    // Initialize component
  }

  // Status and formatting methods
  getStatusIcon(): string {
    return this.data.success ? 'check_circle' : 'error';
  }

  getStatusColor(): string {
    return this.data.success ? 'success' : 'error';
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

  getLogLevelColor(level: string): string {
    switch (level?.toLowerCase()) {
      case 'error': return 'warn';
      case 'warn': return 'accent';
      case 'info': return 'primary';
      case 'debug': return '';
      default: return '';
    }
  }

  getActionIcon(actionType: string): string {
    switch (actionType?.toLowerCase()) {
      case 'click': return 'touch_app';
      case 'type': return 'keyboard';
      case 'navigate': return 'navigation';
      case 'wait': return 'schedule';
      case 'scroll': return 'unfold_more';
      case 'screenshot': return 'camera_alt';
      case 'hover': return 'mouse';
      default: return 'play_arrow';
    }
  }

  getNetworkStatusColor(status: number): string {
    if (status >= 200 && status < 300) return 'success';
    if (status >= 300 && status < 400) return 'accent';
    if (status >= 400) return 'warn';
    return '';
  }

  formatDuration(milliseconds: number): string {
    if (milliseconds < 1000) {
      return `${milliseconds}ms`;
    }
    
    const seconds = Math.floor(milliseconds / 1000);
    if (seconds < 60) {
      return `${seconds}s`;
    }
    
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    return `${minutes}m ${remainingSeconds}s`;
  }

  formatTimestamp(timestamp: Date): string {
    return new Date(timestamp).toLocaleString();
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  // Action methods
  viewScreenshot(screenshot: string): void {
    const newWindow = window.open();
    if (newWindow) {
      newWindow.document.write(`<img src="data:image/png;base64,${screenshot}" style="max-width: 100%; max-height: 100%;" />`);
    }
  }

  exportResults(): void {
    const exportData = {
      testDetails: {
        id: this.data.id,
        name: this.data.name,
        testUrl: this.data.testUrl,
        success: this.data.success,
        executedAt: this.data.executedAt,
        duration: this.data.duration,
        actionsExecuted: this.data.actionsExecuted,
        errorCount: this.data.errorCount
      },
      logs: this.data.logs,
      actions: this.data.actions,
      metrics: this.data.metrics,
      networkRequests: this.data.networkRequests
    };

    const dataStr = JSON.stringify(exportData, null, 2);
    const dataBlob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(dataBlob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `test-result-${this.data.id}.json`;
    link.click();
    URL.revokeObjectURL(url);
  }

  close(): void {
    this.dialogRef.close();
  }

  // Utility getters
  get successRate(): number {
    if (this.data.actions.length === 0) return 100;
    const successfulActions = this.data.actions.filter(a => a.success).length;
    return Math.round((successfulActions / this.data.actions.length) * 100);
  }

  get averageActionDuration(): number {
    if (this.data.actions.length === 0) return 0;
    const totalDuration = this.data.actions.reduce((sum, action) => sum + action.duration, 0);
    return Math.round(totalDuration / this.data.actions.length);
  }

  get totalNetworkSize(): number {
    return this.data.networkRequests.reduce((sum, req) => sum + req.size, 0);
  }

  get averageResponseTime(): number {
    if (this.data.networkRequests.length === 0) return 0;
    const totalTime = this.data.networkRequests.reduce((sum, req) => sum + req.responseTime, 0);
    return Math.round(totalTime / this.data.networkRequests.length);
  }
} 