import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TestResultDetails } from './test-result-details-dialog.component';

export interface TestComparisonData {
  firstResult: TestResultDetails;
  secondResult: TestResultDetails;
  comparison: ComparisonMetrics;
}

export interface ComparisonMetrics {
  durationDifference: number;
  durationPercentageChange: number;
  actionsDifference: number;
  errorsDifference: number;
  successRateDifference: number;
  performanceMetrics: PerformanceComparison;
  actionComparisons: ActionComparison[];
}

export interface PerformanceComparison {
  loadTimeDifference: number;
  domContentLoadedDifference: number;
  firstContentfulPaintDifference: number;
  largestContentfulPaintDifference: number;
  networkRequestsDifference: number;
  memoryUsageDifference?: number;
}

export interface ActionComparison {
  index: number;
  actionType: string;
  firstResult: {
    success: boolean;
    duration: number;
    errorMessage?: string;
  };
  secondResult: {
    success: boolean;
    duration: number;
    errorMessage?: string;
  };
  durationDifference: number;
  statusChanged: boolean;
}

@Component({
  selector: 'app-test-result-comparison-dialog',
  templateUrl: './test-result-comparison-dialog.component.html',
  styleUrls: ['./test-result-comparison-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatCardModule,
    MatTableModule,
    MatProgressBarModule,
    MatDividerModule,
    MatChipsModule,
    MatTooltipModule
  ]
})
export class TestResultComparisonDialogComponent implements OnInit {
  // Expose Math to template
  Math = Math;
  
  selectedTabIndex = 0;
  actionComparisonColumns = ['index', 'actionType', 'firstResult', 'secondResult', 'difference'];

  constructor(
    public dialogRef: MatDialogRef<TestResultComparisonDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TestComparisonData
  ) {}

  ngOnInit(): void {
    // Initialize component
  }

  // Formatting methods
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

  formatPercentage(value: number): string {
    const sign = value > 0 ? '+' : '';
    return `${sign}${value.toFixed(1)}%`;
  }

  formatDifference(value: number, unit: string = 'ms'): string {
    const sign = value > 0 ? '+' : '';
    return `${sign}${value}${unit}`;
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  // Status and color methods
  getComparisonIcon(value: number): string {
    if (value > 0) return 'trending_up';
    if (value < 0) return 'trending_down';
    return 'trending_flat';
  }

  getComparisonColor(value: number, isImprovement: boolean = false): string {
    if (value === 0) return '';
    
    if (isImprovement) {
      return value > 0 ? 'primary' : 'warn';
    } else {
      return value > 0 ? 'warn' : 'primary';
    }
  }

  getStatusIcon(success: boolean): string {
    return success ? 'check_circle' : 'error';
  }

  getStatusColor(success: boolean): string {
    return success ? 'primary' : 'warn';
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

  // Analysis methods
  getOverallPerformanceChange(): 'improved' | 'degraded' | 'neutral' {
    const metrics = this.data.comparison;
    
    // Calculate weighted score based on key metrics
    let score = 0;
    
    // Duration improvement is weighted heavily
    if (metrics.durationPercentageChange < -5) score += 2;
    else if (metrics.durationPercentageChange > 5) score -= 2;
    
    // Success rate improvement
    if (metrics.successRateDifference > 5) score += 1;
    else if (metrics.successRateDifference < -5) score -= 1;
    
    // Error count improvement
    if (metrics.errorsDifference < 0) score += 1;
    else if (metrics.errorsDifference > 0) score -= 1;
    
    if (score > 0) return 'improved';
    if (score < 0) return 'degraded';
    return 'neutral';
  }

  getPerformanceChangeIcon(): string {
    const change = this.getOverallPerformanceChange();
    switch (change) {
      case 'improved': return 'trending_up';
      case 'degraded': return 'trending_down';
      default: return 'trending_flat';
    }
  }

  getPerformanceChangeColor(): string {
    const change = this.getOverallPerformanceChange();
    switch (change) {
      case 'improved': return 'primary';
      case 'degraded': return 'warn';
      default: return 'accent';
    }
  }

  getPerformanceChangeText(): string {
    const change = this.getOverallPerformanceChange();
    switch (change) {
      case 'improved': return 'Performance Improved';
      case 'degraded': return 'Performance Degraded';
      default: return 'Performance Similar';
    }
  }

  // Action methods
  exportComparison(): void {
    const exportData = {
      comparisonDate: new Date().toISOString(),
      firstResult: {
        id: this.data.firstResult.id,
        name: this.data.firstResult.name,
        testUrl: this.data.firstResult.testUrl,
        executedAt: this.data.firstResult.executedAt,
        duration: this.data.firstResult.duration,
        success: this.data.firstResult.success,
        actionsExecuted: this.data.firstResult.actionsExecuted,
        errorCount: this.data.firstResult.errorCount
      },
      secondResult: {
        id: this.data.secondResult.id,
        name: this.data.secondResult.name,
        testUrl: this.data.secondResult.testUrl,
        executedAt: this.data.secondResult.executedAt,
        duration: this.data.secondResult.duration,
        success: this.data.secondResult.success,
        actionsExecuted: this.data.secondResult.actionsExecuted,
        errorCount: this.data.secondResult.errorCount
      },
      comparison: this.data.comparison,
      analysis: {
        overallChange: this.getOverallPerformanceChange(),
        performanceChangeText: this.getPerformanceChangeText()
      }
    };

    const dataStr = JSON.stringify(exportData, null, 2);
    const dataBlob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(dataBlob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `test-comparison-${Date.now()}.json`;
    link.click();
    URL.revokeObjectURL(url);
  }

  close(): void {
    this.dialogRef.close();
  }

  // Utility getters
  get firstSuccessRate(): number {
    if (this.data.firstResult.actions.length === 0) return 100;
    const successfulActions = this.data.firstResult.actions.filter(a => a.success).length;
    return Math.round((successfulActions / this.data.firstResult.actions.length) * 100);
  }

  get secondSuccessRate(): number {
    if (this.data.secondResult.actions.length === 0) return 100;
    const successfulActions = this.data.secondResult.actions.filter(a => a.success).length;
    return Math.round((successfulActions / this.data.secondResult.actions.length) * 100);
  }

  get significantActions(): ActionComparison[] {
    return this.data.comparison.actionComparisons.filter(ac => 
      ac.statusChanged || Math.abs(ac.durationDifference) > 1000
    );
  }
}
