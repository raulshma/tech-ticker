import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, takeUntil } from 'rxjs';
import { ScraperLogsService } from '../../services/scraper-logs.service';
import { ScraperRunLogDto } from '../../../../shared/api/api-client';
import { Location } from '@angular/common';
import { ProductSpecification } from '../../../../shared/components/product-specifications/product-specifications.component';

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

  getParsedSpecifications(): ProductSpecification | undefined {
    if (!this.scraperLog?.specificationData) return undefined;
    try {
      const rawSpecs = JSON.parse(this.scraperLog.specificationData);
      const metadata = this.getParsedSpecificationMetadata();

      // Create a properly formatted ProductSpecification object
      return {
        isSuccess: !this.scraperLog.specificationError, // No error means success
        errorMessage: this.scraperLog.specificationError || undefined,
        specifications: rawSpecs,
        typedSpecifications: this.convertToTypedSpecifications(rawSpecs),
        categorizedSpecs: this.categorizeSpecifications(rawSpecs),
        metadata: metadata ? {
          totalRows: metadata.totalRows || 0,
          dataRows: metadata.dataRows || 0,
          headerRows: metadata.headerRows || 0,
          continuationRows: metadata.continuationRows || 0,
          inlineValueCount: metadata.inlineValueCount || 0,
          multiValueSpecs: metadata.multiValueSpecs || 0,
          structure: metadata.tableStructure || 'Unknown',
          warnings: metadata.warnings || [],
          processingTimeMs: metadata.processingTimeMs || 0
        } : {
          totalRows: 0,
          dataRows: 0,
          headerRows: 0,
          continuationRows: 0,
          inlineValueCount: 0,
          multiValueSpecs: 0,
          structure: 'Unknown',
          warnings: [],
          processingTimeMs: 0
        },
        quality: {
          overallScore: this.scraperLog.specificationQualityScore || 0,
          structureConfidence: metadata?.confidence || 0,
          typeDetectionAccuracy: 0.8, // Default value
          completenessScore: 0.8 // Default value
        },
        parsingTimeMs: this.scraperLog.specificationParsingTime || 0
      };
    } catch (error) {
      console.error('Error parsing specification data:', error);
      return {
        isSuccess: false,
        errorMessage: 'Failed to parse specification data: ' + error,
        specifications: {},
        typedSpecifications: {},
        categorizedSpecs: {},
        metadata: {
          totalRows: 0,
          dataRows: 0,
          headerRows: 0,
          continuationRows: 0,
          inlineValueCount: 0,
          multiValueSpecs: 0,
          structure: 'Error',
          warnings: ['Parsing failed'],
          processingTimeMs: 0
        },
        quality: {
          overallScore: 0,
          structureConfidence: 0,
          typeDetectionAccuracy: 0,
          completenessScore: 0
        },
        parsingTimeMs: 0
      };
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

  private convertToTypedSpecifications(rawSpecs: any): { [key: string]: any } {
    const typedSpecs: { [key: string]: any } = {};

    Object.entries(rawSpecs).forEach(([key, value]) => {
      typedSpecs[key] = {
        value: value,
        type: this.detectSpecificationType(value),
        unit: '',
        numericValue: this.extractNumericValue(value),
        confidence: 0.8,
        category: this.categorizeSpec(key),
        hasMultipleValues: Array.isArray(value) || (typeof value === 'object' && value !== null && typeof value !== 'string'),
        valueCount: Array.isArray(value) ? value.length : 1,
        alternatives: []
      };
    });

    return typedSpecs;
  }

  private categorizeSpecifications(rawSpecs: any): { [key: string]: any } {
    const categories: { [key: string]: any } = {};

    Object.entries(rawSpecs).forEach(([key, value]) => {
      const category = this.categorizeSpec(key);

      if (!categories[category]) {
        categories[category] = {
          name: category,
          specifications: {},
          order: this.getCategoryOrder(category),
          confidence: 0.8,
          isExplicit: true,
          itemCount: 0,
          multiValueCount: 0
        };
      }

      categories[category].specifications[key] = {
        value: value,
        type: this.detectSpecificationType(value),
        unit: '',
        numericValue: this.extractNumericValue(value),
        confidence: 0.8,
        category: category,
        hasMultipleValues: Array.isArray(value) || (typeof value === 'object' && value !== null && typeof value !== 'string'),
        valueCount: Array.isArray(value) ? value.length : 1,
        alternatives: []
      };

      categories[category].itemCount++;
      if (Array.isArray(value) || (typeof value === 'object' && value !== null && typeof value !== 'string')) {
        categories[category].multiValueCount++;
      }
    });

    return categories;
  }

  private detectSpecificationType(value: any): string {
    if (Array.isArray(value)) return 'List';
    if (typeof value === 'number') return 'Numeric';
    if (typeof value === 'string') {
      if (value.match(/\d+(\.\d+)?\s*(gb|mb|tb|ghz|mhz|w|v|a)/i)) return 'Numeric';
      if (value.match(/\d+x\d+/i)) return 'Resolution';
      if (value.toLowerCase().includes('clock')) return 'Clock';
      if (value.toLowerCase().includes('memory')) return 'Memory';
      if (value.toLowerCase().includes('power')) return 'Power';
      if (value.toLowerCase().includes('port') || value.toLowerCase().includes('interface')) return 'Interface';
    }
    return 'Text';
  }

  private extractNumericValue(value: any): number | undefined {
    if (typeof value === 'number') return value;
    if (typeof value === 'string') {
      const match = value.match(/(\d+(?:\.\d+)?)/);
      return match ? parseFloat(match[1]) : undefined;
    }
    return undefined;
  }

  private categorizeSpec(key: string): string {
    const lowerKey = key.toLowerCase();

    if (lowerKey.includes('memory') || lowerKey.includes('ram')) return 'Memory';
    if (lowerKey.includes('clock') || lowerKey.includes('speed') || lowerKey.includes('frequency')) return 'Performance';
    if (lowerKey.includes('power') || lowerKey.includes('watt') || lowerKey.includes('consumption')) return 'Power';
    if (lowerKey.includes('interface') || lowerKey.includes('port') || lowerKey.includes('connector')) return 'Connectivity';
    if (lowerKey.includes('dimension') || lowerKey.includes('size') || lowerKey.includes('weight')) return 'Physical';
    if (lowerKey.includes('resolution') || lowerKey.includes('display')) return 'Display';
    if (lowerKey.includes('brand') || lowerKey.includes('model') || lowerKey.includes('series')) return 'General';
    if (lowerKey.includes('warranty') || lowerKey.includes('support')) return 'Support';

    return 'Other';
  }

  private getCategoryOrder(category: string): number {
    const order: { [key: string]: number } = {
      'General': 1,
      'Performance': 2,
      'Memory': 3,
      'Display': 4,
      'Connectivity': 5,
      'Power': 6,
      'Physical': 7,
      'Support': 8,
      'Other': 9
    };

    return order[category] || 10;
  }
}
