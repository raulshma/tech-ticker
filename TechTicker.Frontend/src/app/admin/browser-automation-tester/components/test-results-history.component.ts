import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { SelectionModel } from '@angular/cdk/collections';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { TechTickerApiClient } from '../../../shared/api/api-client';

// Define interfaces for the component
interface SavedTestResult {
  id: string;
  name: string;
  description?: string;
  tags?: string[];
  testUrl: string;
  success: boolean;
  savedAt: Date;
  executedAt: Date;
  duration: number;
  actionsExecuted: number;
  errorCount: number;
  profileHash: string;
  createdBy: string;
}

interface TestStatistics {
  totalTests: number;
  successfulTests: number;
  failedTests: number;
  successRate: number;
  averageExecutionTime: number;
  medianExecutionTime: number;
  totalActionsExecuted: number;
  firstTestDate?: Date;
  lastTestDate?: Date;
  uniqueUrls: number;
  uniqueProfiles: number;
}

@Component({
  selector: 'app-test-results-history',
  templateUrl: './test-results-history.component.html',
  styleUrls: ['./test-results-history.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatCheckboxModule,
    MatMenuModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatBadgeModule,
    MatDividerModule
  ]
})
export class TestResultsHistoryComponent implements OnInit, OnDestroy {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  // Data and state
  dataSource = new MatTableDataSource<SavedTestResult>([]);
  selection = new SelectionModel<SavedTestResult>(true, []);
  isLoading = false;
  statistics: TestStatistics | null = null;
  availableTags: string[] = [];
  
  // Pagination
  totalCount = 0;
  pageSize = 20;
  pageNumber = 1;
  
  // Filtering and search
  filterForm: FormGroup;
  searchSubject = new Subject<string>();
  selectedTags: string[] = [];
  
  // Table configuration
  displayedColumns: string[] = [
    'select',
    'status',
    'name',
    'testUrl',
    'duration',
    'actionsExecuted',
    'errorCount',
    'executedAt',
    'actions'
  ];
  
  // Comparison
  comparisonMode = false;
  selectedForComparison: SavedTestResult[] = [];
  
  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private apiClient: TechTickerApiClient,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {
    this.filterForm = this.fb.group({
      searchTerm: [''],
      tags: [[]]
    });
  }

  ngOnInit(): void {
    this.setupSearchSubscription();
    this.loadTestResults();
    this.loadStatistics();
    this.loadAvailableTags();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupSearchSubscription(): void {
    this.searchSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.pageNumber = 1;
        this.loadTestResults();
      });
  }

  async loadTestResults(): Promise<void> {
    try {
      this.isLoading = true;
      
      const searchTerm = this.filterForm.get('searchTerm')?.value || '';
      const tags = this.selectedTags.length > 0 ? this.selectedTags.join(',') : undefined;
      
      // TODO: Replace with actual API call when client is updated
      // const response = await this.apiClient.getSavedTestResults(
      //   this.pageNumber,
      //   this.pageSize,
      //   searchTerm || undefined,
      //   tags
      // ).toPromise();
      const response = await this.mockApiMethods.getSavedTestResults();
      
      if (response?.success && response.data) {
        this.dataSource.data = response.data.data || [];
        this.totalCount = response.data.totalCount || 0;
        this.statistics = response.data.statistics || null;
        this.availableTags = response.data.availableTags || [];
      }
    } catch (error) {
      console.error('Error loading test results:', error);
      this.showErrorMessage('Failed to load test results');
    } finally {
      this.isLoading = false;
    }
  }

  async loadStatistics(): Promise<void> {
    try {
      // TODO: Replace with actual API call when client is updated
      const response = await this.mockApiMethods.getTestStatistics();
      if (response?.success && response.data) {
        this.statistics = response.data;
      }
    } catch (error) {
      console.error('Error loading statistics:', error);
    }
  }

  async loadAvailableTags(): Promise<void> {
    try {
      // TODO: Replace with actual API call when client is updated
      const response = await this.mockApiMethods.getAvailableTags();
      if (response?.success && response.data) {
        this.availableTags = response.data;
      }
    } catch (error) {
      console.error('Error loading available tags:', error);
    }
  }

  onSearchChange(): void {
    const searchTerm = this.filterForm.get('searchTerm')?.value || '';
    this.searchSubject.next(searchTerm);
  }

  onTagSelectionChange(): void {
    this.pageNumber = 1;
    this.loadTestResults();
  }

  onPageChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.pageNumber = event.pageIndex + 1;
    this.loadTestResults();
  }

  // Selection management
  isAllSelected(): boolean {
    const numSelected = this.selection.selected.length;
    const numRows = this.dataSource.data.length;
    return numSelected === numRows;
  }

  masterToggle(): void {
    this.isAllSelected()
      ? this.selection.clear()
      : this.dataSource.data.forEach(row => this.selection.select(row));
    
    this.updateComparisonSelection();
  }

  toggleSelection(row: SavedTestResult): void {
    this.selection.toggle(row);
    this.updateComparisonSelection();
  }

  private updateComparisonSelection(): void {
    if (this.comparisonMode) {
      this.selectedForComparison = this.selection.selected.slice(0, 2);
    }
  }

  // Comparison functionality
  enableComparisonMode(): void {
    this.comparisonMode = true;
    this.selection.clear();
    this.selectedForComparison = [];
    this.showSuccessMessage('Select exactly 2 results to compare');
  }

  disableComparisonMode(): void {
    this.comparisonMode = false;
    this.selection.clear();
    this.selectedForComparison = [];
  }

  canCompare(): boolean {
    return this.comparisonMode && this.selectedForComparison.length === 2;
  }

  async compareSelected(): Promise<void> {
    if (!this.canCompare()) {
      this.showErrorMessage('Please select exactly 2 results to compare');
      return;
    }

    try {
      const [first, second] = this.selectedForComparison;
      const request = {
        firstResultId: first.id,
        secondResultId: second.id,
        includeScreenshots: false,
        includeNetworkData: false,
        includeDetailedDifferences: true
      };

      // TODO: Replace with actual API call when client is updated
      const response = await this.mockApiMethods.compareTestResults();
      
      if (response?.success && response.data) {
        // Open comparison dialog
        this.openComparisonDialog(response.data);
      }
    } catch (error) {
      console.error('Error comparing test results:', error);
      this.showErrorMessage('Failed to compare test results');
    }
  }

  // Actions
  async viewDetails(result: SavedTestResult): Promise<void> {
    try {
      // TODO: Replace with actual API call when client is updated
      const response = await this.mockApiMethods.getSavedTestResult();
      
      if (response?.success && response.data) {
        this.openDetailsDialog(response.data);
      }
    } catch (error) {
      console.error('Error loading test result details:', error);
      this.showErrorMessage('Failed to load test result details');
    }
  }

  async exportResult(result: SavedTestResult, format: 'json' | 'csv' | 'pdf' = 'json'): Promise<void> {
    try {
      // For now, we'll call the export endpoint and handle the download
      // TODO: Replace with actual API call when client is updated
      const response = await this.mockApiMethods.exportTestResult();
      this.showSuccessMessage(`Export initiated for ${result.name}`);
    } catch (error) {
      console.error('Error exporting test result:', error);
      this.showErrorMessage('Failed to export test result');
    }
  }

  async deleteResult(result: SavedTestResult): Promise<void> {
    if (!confirm(`Are you sure you want to delete "${result.name}"?`)) {
      return;
    }

    try {
      // TODO: Replace with actual API call when client is updated
      const response = await this.mockApiMethods.deleteSavedTestResult();
      
      if (response?.success) {
        this.showSuccessMessage('Test result deleted successfully');
        this.loadTestResults();
        this.loadStatistics();
      }
    } catch (error) {
      console.error('Error deleting test result:', error);
      this.showErrorMessage('Failed to delete test result');
    }
  }

  async bulkDelete(): Promise<void> {
    const selected = this.selection.selected;
    if (selected.length === 0) {
      this.showErrorMessage('Please select results to delete');
      return;
    }

    if (!confirm(`Are you sure you want to delete ${selected.length} test result(s)?`)) {
      return;
    }

    try {
      const request = {
        resultIds: selected.map(r => r.id)
      };

      // TODO: Replace with actual API call when client is updated
      const response = await this.mockApiMethods.bulkDeleteSavedTestResults();
      
      if (response?.success && response.data) {
        const result = response.data;
        this.showSuccessMessage(
          `Successfully deleted ${result.successfullyDeleted} of ${result.totalRequested} test results`
        );
        
        this.selection.clear();
        this.loadTestResults();
        this.loadStatistics();
      }
    } catch (error) {
      console.error('Error bulk deleting test results:', error);
      this.showErrorMessage('Failed to delete test results');
    }
  }

  // Utility methods
  getStatusIcon(success: boolean): string {
    return success ? 'check_circle' : 'error';
  }

  getStatusColor(success: boolean): string {
    return success ? 'success' : 'error';
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

  formatDate(date: Date): string {
    return new Date(date).toLocaleString();
  }

  getSuccessRateColor(rate: number): string {
    if (rate >= 90) return 'success';
    if (rate >= 70) return 'warning';
    return 'error';
  }

  // Dialog methods
  private openDetailsDialog(details: any): void {
    // TODO: Implement details dialog
    console.log('Opening details dialog for:', details);
  }

  private openComparisonDialog(comparison: any): void {
    // TODO: Implement comparison dialog
    console.log('Opening comparison dialog for:', comparison);
  }

  // Notification methods
  private showSuccessMessage(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: 'success-snackbar'
    });
  }

  private showErrorMessage(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: 'error-snackbar'
    });
  }

  // Template helpers
  get hasSelection(): boolean {
    return this.selection.selected.length > 0;
  }

  get selectionCount(): number {
    return this.selection.selected.length;
  }

  get hasStatistics(): boolean {
    return this.statistics !== null;
  }

  // Add method signatures that might be missing
  // Mock API methods until the API client is regenerated
  private mockApiMethods = {
    getSavedTestResults: () => Promise.resolve({
      success: true,
      data: {
        data: [],
        totalCount: 0,
        pageNumber: 1,
        pageSize: 20,
        totalPages: 0,
        hasNextPage: false,
        hasPreviousPage: false,
        availableTags: [],
        statistics: {
          totalTests: 0,
          successfulTests: 0,
          failedTests: 0,
          successRate: 0,
          averageExecutionTime: 0,
          medianExecutionTime: 0,
          totalActionsExecuted: 0,
          uniqueUrls: 0,
          uniqueProfiles: 0
        }
      }
    }),
    getTestStatistics: () => Promise.resolve({
      success: true,
      data: {
        totalTests: 0,
        successfulTests: 0,
        failedTests: 0,
        successRate: 0,
        averageExecutionTime: 0,
        medianExecutionTime: 0,
        totalActionsExecuted: 0,
        uniqueUrls: 0,
        uniqueProfiles: 0
      }
    }),
    getAvailableTags: () => Promise.resolve({
      success: true,
      data: []
    }),
    getSavedTestResult: () => Promise.resolve({
      success: true,
      data: null
    }),
    exportTestResult: () => Promise.resolve({
      success: true,
      data: null
    }),
    deleteSavedTestResult: () => Promise.resolve({
      success: true,
      data: true
    }),
    bulkDeleteSavedTestResults: () => Promise.resolve({
      success: true,
      data: {
        totalRequested: 0,
        successfullyDeleted: 0,
        failedToDelete: 0,
        errors: []
      }
    }),
    compareTestResults: () => Promise.resolve({
      success: true,
      data: null
    })
  };
} 