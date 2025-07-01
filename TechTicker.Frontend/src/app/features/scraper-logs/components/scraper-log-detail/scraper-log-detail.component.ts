import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, takeUntil } from 'rxjs';
import { ScraperLogsService } from '../../services/scraper-logs.service';
import { ScraperRunLogDto } from '../../../../shared/api/api-client';
import { Location } from '@angular/common';

@Component({
  selector: 'app-scraper-log-detail',
  templateUrl: './scraper-log-detail.component.html',
  styleUrls: ['./scraper-log-detail.component.scss'],
  standalone: false
})
export class ScraperLogDetailComponent implements OnInit, OnDestroy {
  scraperLog: ScraperRunLogDto | null = null;
  isLoading = false;
  runId: string | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private scraperLogsService: ScraperLogsService,
    private snackBar: MatSnackBar,
    private location: Location
  ) { }

  ngOnInit(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.runId = params['runId'];
      if (this.runId) {
        this.loadScraperLogDetail();
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadScraperLogDetail(): void {
    if (!this.runId) return;

    this.isLoading = true;
    this.scraperLogsService.getScraperLogDetail(this.runId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (log) => {
          this.isLoading = false;
          if (log) {
            this.scraperLog = log;
          } else {
            this.snackBar.open('Scraper log not found', 'Close', { duration: 3000 });
            this.router.navigate(['/scraper-logs']);
          }
        },
        error: (error) => {
          this.isLoading = false;
          console.error('Error loading scraper log detail:', error);
          this.snackBar.open('Error loading scraper log details', 'Close', { duration: 3000 });
        }
      });
  }

  goBack(): void {
    this.location.back();
  }

  copyToClipboard(text: string): void {
    navigator.clipboard.writeText(text).then(() => {
      this.snackBar.open('Copied to clipboard', 'Close', { duration: 2000 });
    }).catch(err => {
      console.error('Could not copy text: ', err);
      this.snackBar.open('Failed to copy to clipboard', 'Close', { duration: 3000 });
    });
  }

  openUrl(url: string): void {
    window.open(url, '_blank');
  }

  formatDate(date: Date | string | undefined): string {
    if (!date) return '';
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    return dateObj.toLocaleString();
  }

  formatDuration(duration: string | undefined): string {
    if (!duration) return '';
    // Duration might be in format like "00:00:30.123" or just seconds
    if (duration.includes(':')) {
      return duration;
    }
    // If it's just a number (seconds), format it nicely
    const seconds = parseFloat(duration);
    if (isNaN(seconds)) return duration;
    
    if (seconds < 1) {
      return `${Math.round(seconds * 1000)}ms`;
    } else if (seconds < 60) {
      return `${seconds.toFixed(2)}s`;
    } else {
      const minutes = Math.floor(seconds / 60);
      const remainingSeconds = seconds % 60;
      return `${minutes}m ${remainingSeconds.toFixed(2)}s`;
    }
  }

  formatBytes(bytes: number | undefined): string {
    if (!bytes) return '';
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return `${(bytes / Math.pow(1024, i)).toFixed(2)} ${sizes[i]}`;
  }

  formatJson(obj: any): string {
    if (!obj) return '';
    try {
      if (typeof obj === 'string') {
        // Try to parse if it's a JSON string
        const parsed = JSON.parse(obj);
        return JSON.stringify(parsed, null, 2);
      }
      return JSON.stringify(obj, null, 2);
    } catch (error) {
      // If parsing fails, return as string
      return typeof obj === 'string' ? obj : JSON.stringify(obj, null, 2);
    }
  }

  getStatusIcon(status: string | undefined): string {
    switch (status?.toUpperCase()) {
      case 'SUCCESS':
        return 'check_circle';
      case 'FAILED':
        return 'error';
      case 'STARTED':
      case 'IN_PROGRESS':
        return 'hourglass_empty';
      case 'TIMEOUT':
        return 'timer_off';
      case 'CANCELLED':
        return 'cancel';
      default:
        return 'help';
    }
  }

  getStatusColor(status: string | undefined): string {
    switch (status?.toUpperCase()) {
      case 'SUCCESS':
        return 'primary';
      case 'FAILED':
        return 'warn';
      case 'STARTED':
      case 'IN_PROGRESS':
        return 'accent';
      case 'TIMEOUT':
      case 'CANCELLED':
        return 'warn';
      default:
        return 'primary';
    }
  }

  getHttpStatusClass(statusCode: number | undefined): string {
    if (!statusCode) return '';
    
    if (statusCode >= 200 && statusCode < 300) {
      return 'http-success';
    } else if (statusCode >= 300 && statusCode < 400) {
      return 'http-redirect';
    } else if (statusCode >= 400 && statusCode < 500) {
      return 'http-client-error';
    } else if (statusCode >= 500) {
      return 'http-server-error';
    }
    return '';
  }

  hasImageData(): boolean {
    return !!(this.scraperLog?.extractedPrimaryImageUrl || 
              this.scraperLog?.extractedAdditionalImageUrls?.length || 
              this.scraperLog?.extractedOriginalImageUrls?.length ||
              this.scraperLog?.imageProcessingCount ||
              this.scraperLog?.imageUploadCount ||
              this.scraperLog?.imageScrapingError);
  }

  hasSpecificationData(): boolean {
    return !!(this.scraperLog?.specificationData || 
              this.scraperLog?.specificationMetadata || 
              this.scraperLog?.specificationCount ||
              this.scraperLog?.specificationError);
  }

  getParsedSpecifications(): any {
    if (!this.scraperLog?.specificationData) return null;
    try {
      return JSON.parse(this.scraperLog.specificationData);
    } catch (error) {
      console.error('Error parsing specification data:', error);
      return null;
    }
  }

  getParsedSpecificationMetadata(): any {
    if (!this.scraperLog?.specificationMetadata) return null;
    try {
      return JSON.parse(this.scraperLog.specificationMetadata);
    } catch (error) {
      console.error('Error parsing specification metadata:', error);
      return null;
    }
  }

  getSpecificationQualityBadgeClass(): string {
    if (!this.scraperLog?.specificationQualityScore) return 'badge-secondary';
    
    const score = this.scraperLog.specificationQualityScore;
    if (score >= 0.9) return 'badge-success';
    if (score >= 0.7) return 'badge-warning';
    return 'badge-danger';
  }

  formatSpecificationParsingTime(): string {
    if (!this.scraperLog?.specificationParsingTime) return '';
    return `${this.scraperLog.specificationParsingTime}ms`;
  }

  getProxyDisplayInfo(): { hasProxy: boolean; proxyInfo: string } {
    if (this.scraperLog?.proxyUsed) {
      return { hasProxy: true, proxyInfo: this.scraperLog.proxyUsed };
    } else if (this.scraperLog?.proxyId) {
      return { hasProxy: true, proxyInfo: `Proxy ID: ${this.scraperLog.proxyId}` };
    } else {
      return { hasProxy: false, proxyInfo: 'Direct Connection' };
    }
  }

  viewRetryDetails(runId: string): void {
    // Navigate to the retry attempt details
    this.router.navigate(['/scraper-logs', runId]);
  }
}
