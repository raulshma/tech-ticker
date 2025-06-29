import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';
import { ScraperLogsService, ScraperLogFilter, ScraperLogPagedResult } from '../../services/scraper-logs.service';
import { ScraperRunLogSummaryDto } from '../../../../shared/api/api-client';

@Component({
  selector: 'app-scraper-logs-list',
  templateUrl: './scraper-logs-list.component.html',
  styleUrls: ['./scraper-logs-list.component.scss'],
  standalone: false
})
export class ScraperLogsListComponent implements OnInit, OnDestroy {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  displayedColumns: string[] = [
    'startedAt',
    'status',
    'duration',
    'sellerName',
    'productName',
    'proxyInfo',
    'errorMessage',
    'actions'
  ];

  dataSource: ScraperRunLogSummaryDto[] = [];
  totalCount = 0;
  pageSize = 10;
  pageIndex = 0;
  isLoading = false;

  filterForm: FormGroup;
  statusOptions = [
    { value: '', label: 'All Statuses' },
    { value: 'SUCCESS', label: 'Success' },
    { value: 'FAILED', label: 'Failed' },
    { value: 'STARTED', label: 'Started' },
    { value: 'IN_PROGRESS', label: 'In Progress' },
    { value: 'TIMEOUT', label: 'Timeout' },
    { value: 'CANCELLED', label: 'Cancelled' }
  ];

  errorCategoryOptions = [
    { value: '', label: 'All Categories' },
    { value: 'NETWORK', label: 'Network' },
    { value: 'PARSING', label: 'Parsing' },
    { value: 'TIMEOUT', label: 'Timeout' },
    { value: 'SELECTOR', label: 'Selector' },
    { value: 'AUTHENTICATION', label: 'Authentication' }
  ];

  private destroy$ = new Subject<void>();

  constructor(
    private scraperLogsService: ScraperLogsService,
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    private router: Router
  ) {
    this.filterForm = this.fb.group({
      mappingId: [''],
      status: [''],
      errorCategory: [''],
      dateFrom: [null],
      dateTo: [null],
      sellerName: ['']
    });
  }

  ngOnInit(): void {
    this.loadScraperLogs();
    this.setupFilterSubscription();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupFilterSubscription(): void {
    this.filterForm.valueChanges
      .pipe(
        takeUntil(this.destroy$),
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.pageIndex = 0;
        this.loadScraperLogs();
      });
  }

  loadScraperLogs(): void {
    this.isLoading = true;

    const filter: ScraperLogFilter = {
      page: this.pageIndex + 1, // API uses 1-based pagination
      pageSize: this.pageSize,
      ...this.filterForm.value
    };

    // Remove empty values
    Object.keys(filter).forEach(key => {
      if (filter[key as keyof ScraperLogFilter] === '' || filter[key as keyof ScraperLogFilter] === null) {
        delete filter[key as keyof ScraperLogFilter];
      }
    });

    this.scraperLogsService.getScraperLogs(filter)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result: ScraperLogPagedResult | null) => {
          this.isLoading = false;
          if (result) {
            this.dataSource = result.items;
            this.totalCount = result.totalCount;
          } else {
            this.dataSource = [];
            this.totalCount = 0;
            this.snackBar.open('Failed to load scraper logs', 'Close', { duration: 3000 });
          }
        },
        error: (error) => {
          this.isLoading = false;
          console.error('Error loading scraper logs:', error);
          this.snackBar.open('Error loading scraper logs', 'Close', { duration: 3000 });
        }
      });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadScraperLogs();
  }

  clearFilters(): void {
    this.filterForm.reset();
    this.pageIndex = 0;
    this.loadScraperLogs();
  }

  viewDetail(runId: string): void {
    this.router.navigate(['/scraper-logs', runId]);
  }

  // Statistics methods for the new template
  getSuccessCount(): number {
    return this.dataSource.filter(log => 
      log.status === 'SUCCESS' || log.status === 'Completed'
    ).length;
  }

  getFailedCount(): number {
    return this.dataSource.filter(log => 
      log.status === 'FAILED' || log.status === 'Error'
    ).length;
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

  truncateText(text?: string, maxLength: number = 50): string {
    if (!text) return 'N/A';
    return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
  }
}
