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
import { TestResultDetailsDialogComponent } from './test-result-details-dialog.component';
import { TestResultComparisonDialogComponent } from './test-result-comparison-dialog.component';

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
      
      const response = await this.apiClient.getSavedTestResults(
        this.pageNumber,
        this.pageSize,
        searchTerm || undefined,
        tags
      ).toPromise();
      
      if (response?.success && response.data) {
        // Map the API response to our local interface format
        this.dataSource.data = response.data.map(dto => this.mapSavedTestResultDto(dto));
        this.totalCount = response.pagination?.totalCount || 0;
        
        // Load statistics and tags separately since they might not be included in the paginated response
        await this.loadStatistics();
        await this.loadAvailableTags();
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
      const response = await this.apiClient.getTestStatistics().toPromise();
      if (response?.success && response.data) {
        this.statistics = {
          totalTests: response.data.totalTests || 0,
          successfulTests: response.data.successfulTests || 0,
          failedTests: response.data.failedTests || 0,
          successRate: response.data.successRate || 0,
          averageExecutionTime: response.data.averageExecutionTime || 0,
          medianExecutionTime: response.data.medianExecutionTime || 0,
          totalActionsExecuted: response.data.totalActionsExecuted || 0,
          firstTestDate: response.data.firstTestDate ? new Date(response.data.firstTestDate) : undefined,
          lastTestDate: response.data.lastTestDate ? new Date(response.data.lastTestDate) : undefined,
          uniqueUrls: response.data.uniqueUrls || 0,
          uniqueProfiles: response.data.uniqueProfiles || 0
        };
      }
    } catch (error) {
      console.error('Error loading statistics:', error);
    }
  }

  async loadAvailableTags(): Promise<void> {
    try {
      const response = await this.apiClient.getAvailableTags().toPromise();
      if (response?.success && response.data) {
        this.availableTags = response.data || [];
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
        secondResultId: second.id
      };

      const response = await this.apiClient.compareTestResults(request as any).toPromise();
      
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
      const response = await this.apiClient.getSavedTestResult(result.id).toPromise();
      
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
      await this.apiClient.exportTestResult(result.id, format).toPromise();
      this.showSuccessMessage(`Export initiated for ${result.name}`);
      // The API should return the file or a download URL - handle accordingly
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
      const response = await this.apiClient.deleteSavedTestResult(result.id).toPromise();
      
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

      const response = await this.apiClient.bulkDeleteSavedTestResults(request as any).toPromise();
      
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
    this.dialog.open(TestResultDetailsDialogComponent, {
      width: '90vw',
      maxWidth: '1200px',
      height: '80vh',
      data: details
    });
  }

  private openComparisonDialog(comparison: any): void {
    this.dialog.open(TestResultComparisonDialogComponent, {
      width: '95vw',
      maxWidth: '1400px',
      height: '85vh',
      data: comparison
    });
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

  // Data mapping
  private mapSavedTestResultDto(dto: any): SavedTestResult {
    return {
      id: dto.id || '',
      name: dto.name || 'Unnamed Test',
      description: dto.description,
      tags: dto.tags || [],
      testUrl: dto.testUrl || '',
      success: dto.success || false,
      savedAt: dto.savedAt ? new Date(dto.savedAt) : new Date(),
      executedAt: dto.executedAt ? new Date(dto.executedAt) : new Date(),
      duration: dto.duration || 0,
      actionsExecuted: dto.actionsExecuted || 0,
      errorCount: dto.errorCount || 0,
      profileHash: dto.profileHash || '',
      createdBy: dto.createdBy || 'Unknown'
    };
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


} 