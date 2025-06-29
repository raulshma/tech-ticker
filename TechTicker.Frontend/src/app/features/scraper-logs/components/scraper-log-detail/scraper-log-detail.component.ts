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
  retryChain: ScraperRunLogDto[] = [];
  isLoading = false;
  isLoadingRetryChain = false;
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
    this.route.paramMap
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        this.runId = params.get('runId');
        if (this.runId) {
          this.loadScraperLogDetail();
          this.loadRetryChain();
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

  private loadRetryChain(): void {
    if (!this.runId) return;

    this.isLoadingRetryChain = true;
    this.scraperLogsService.getRetryChain(this.runId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (chain) => {
          this.isLoadingRetryChain = false;
          this.retryChain = chain;
        },
        error: (error) => {
          this.isLoadingRetryChain = false;
          console.error('Error loading retry chain:', error);
        }
      });
  }

  goBack(): void {
    this.location.back();
  }

  getStatusColor(status?: string): string {
    return this.scraperLogsService.getStatusColor(status);
  }

  getStatusIcon(status?: string): string {
    return this.scraperLogsService.getStatusIcon(status);
  }

  formatDuration(duration?: string): string {
    return this.scraperLogsService.formatDuration(duration);
  }

  formatDate(date?: Date): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleString();
  }

  formatBytes(bytes?: number): string {
    if (!bytes) return 'N/A';
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
  }

  parseJsonSafely(jsonString?: string): any {
    if (!jsonString) return null;
    try {
      return JSON.parse(jsonString);
    } catch {
      return null;
    }
  }

  formatJson(obj: any): string {
    if (!obj) return 'N/A';
    return JSON.stringify(obj, null, 2);
  }

  formatObjectAsJson(obj: any): string {
    if (!obj) return 'N/A';
    return JSON.stringify(obj, null, 2);
  }

  hasAdditionalHeaders(): boolean {
    return !!(this.scraperLog?.additionalHeaders &&
             (typeof this.scraperLog.additionalHeaders === 'string' ?
              this.parseJsonSafely(this.scraperLog.additionalHeaders) :
              this.scraperLog.additionalHeaders));
  }

  hasSelectors(): boolean {
    return !!(this.scraperLog?.selectors);
  }

  hasImageData(): boolean {
    return !!(this.scraperLog?.extractedPrimaryImageUrl || 
              this.scraperLog?.extractedAdditionalImageUrls?.length || 
              this.scraperLog?.extractedOriginalImageUrls?.length ||
              this.scraperLog?.imageProcessingCount ||
              this.scraperLog?.imageUploadCount ||
              this.scraperLog?.imageScrapingError);
  }

  hasProxyData(): boolean {
    return !!(this.scraperLog?.proxyUsed || this.scraperLog?.proxyId);
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

  getAdditionalHeadersJson(): string {
    if (!this.scraperLog?.additionalHeaders) return 'N/A';
    if (typeof this.scraperLog.additionalHeaders === 'string') {
      return this.formatJson(this.parseJsonSafely(this.scraperLog.additionalHeaders));
    }
    return this.formatObjectAsJson(this.scraperLog.additionalHeaders);
  }

  getSelectorsJson(): string {
    if (!this.scraperLog?.selectors) return 'N/A';
    return this.formatObjectAsJson(this.scraperLog.selectors);
  }

  copyToClipboard(text: string): void {
    navigator.clipboard.writeText(text).then(() => {
      this.snackBar.open('Copied to clipboard', 'Close', { duration: 2000 });
    }).catch(() => {
      this.snackBar.open('Failed to copy to clipboard', 'Close', { duration: 3000 });
    });
  }

  viewRetryDetail(retryRunId: string): void {
    this.router.navigate(['/scraper-logs', retryRunId]);
  }

  getHttpStatusClass(statusCode: number): string {
    if (statusCode >= 200 && statusCode < 300) {
      return 'success-chip';
    } else if (statusCode >= 400 && statusCode < 500) {
      return 'warning-chip';
    } else if (statusCode >= 500) {
      return 'error-chip';
    }
    return 'info-chip';
  }

  openUrl(url: string): void {
    if (url) {
      window.open(url, '_blank', 'noopener,noreferrer');
    }
  }
}
