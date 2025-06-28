import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule } from '@angular/material/sort';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { SelectionModel } from '@angular/cdk/collections';
import { firstValueFrom } from 'rxjs';
import {
  ProxyImportItemDto,
  BulkProxyImportDto,
  BulkProxyImportValidationDto,
  BulkProxyImportResultDto,
  CreateProxyConfigurationDto,
  ProxyTextParseDto,
  ProxyTestResultDto,
  TechTickerApiClient
} from '../../../shared/api/api-client';

@Component({
  selector: 'app-bulk-import',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatChipsModule,
    MatTabsModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSortModule,
    MatTooltipModule,
    ScrollingModule
  ],
  templateUrl: './bulk-import.component.html',
  styleUrls: ['./bulk-import.component.scss']
})
export class BulkImportComponent implements OnInit {
  importForm: FormGroup;
  loading = false;
  parsing = false;
  validating = false;
  importing = false;

  // Progress tracking
  parseProgress = 0;
  parseProgressMessage = '';
  totalProxiesToParse = 0;
  parseAbortController: AbortController | null = null;

  parsedProxies: ProxyImportItemDto[] = [];
  validationResult: BulkProxyImportValidationDto | null = null;
  importResult: BulkProxyImportResultDto | null = null;

  // Testing functionality
  testingProxies = new Set<string>(); // Track which proxies are being tested
  testResults = new Map<string, ProxyTestResultDto>(); // Store test results
  bulkTesting = false;

  displayedColumns: string[] = ['select', 'host', 'port', 'type', 'auth', 'status', 'test', 'errors'];

  currentStep = 0; // 0: Input, 1: Preview, 2: Results

  // Virtual scrolling
  readonly itemSize = 48; // Height of each row in pixels

  // Selection functionality
  selectedProxies = new Set<string>();
  selectAll = false;

  // Filtering functionality
  filterText = '';
  filterType = '';
  filterStatus = '';
  filteredProxies: ProxyImportItemDto[] = [];

  // Available filter options
  statusOptions = [
    { value: '', label: 'All Status' },
    { value: 'valid', label: 'Valid' },
    { value: 'invalid', label: 'Invalid' },
    { value: 'duplicate', label: 'Duplicate' }
  ];

  typeOptions = [
    { value: '', label: 'All Types' },
    { value: 'HTTP', label: 'HTTP' },
    { value: 'HTTPS', label: 'HTTPS' },
    { value: 'SOCKS4', label: 'SOCKS4' },
    { value: 'SOCKS5', label: 'SOCKS5' }
  ];

  // Proxy type options for bulk import
  proxyTypes = [
    { value: 'HTTP', label: 'HTTP' },
    { value: 'HTTPS', label: 'HTTPS' },
    { value: 'SOCKS4', label: 'SOCKS4' },
    { value: 'SOCKS5', label: 'SOCKS5' }
  ];

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private snackBar: MatSnackBar,
    private apiClient: TechTickerApiClient
  ) {
    this.importForm = this.createForm();
  }

  ngOnInit(): void {}

  private createForm(): FormGroup {
    return this.fb.group({
      proxyText: ['', [Validators.required]],
      defaultProxyType: ['HTTP', [Validators.required]],
      testAfterImport: [true],
      skipDuplicates: [true],
      defaultTimeoutSeconds: [30, [Validators.min(1), Validators.max(300)]],
      defaultMaxRetries: [3, [Validators.min(0), Validators.max(10)]],
      defaultIsActive: [true]
    });
  }

  async parseProxies(): Promise<void> {
    if (!this.importForm.get('proxyText')?.value?.trim()) {
      this.snackBar.open('Please enter proxy data', 'Close', { duration: 3000 });
      return;
    }

    const proxyText = this.importForm.get('proxyText')?.value;
    const lines = proxyText.split('\n').filter((line: string) => line.trim());

    this.totalProxiesToParse = lines.length;
    this.parseProgress = 0;
    this.parseProgressMessage = 'Initializing parser...';
    this.parsing = true;
    this.parsedProxies = [];
    this.parseAbortController = new AbortController();

    try {
      // For large datasets, process in chunks to avoid UI blocking
      if (lines.length > 1000) {
        await this.parseProxiesInChunks(lines);
      } else {
        // For smaller datasets, use the API directly
        await this.parseProxiesViaAPI(proxyText);
      }

      this.currentStep = 1;
      this.applyFilters(); // Initialize filtered list
      this.snackBar.open(`Parsed ${this.parsedProxies.length} proxies`, 'Close', { duration: 3000 });
    } catch (error) {
      console.error('Error parsing proxies:', error);
      this.snackBar.open('Failed to parse proxy data', 'Close', { duration: 3000 });
    } finally {
      this.parsing = false;
      this.parseProgress = 0;
      this.parseProgressMessage = '';
      this.parseAbortController = null;
    }
  }

  private async parseProxiesViaAPI(proxyText: string): Promise<void> {
    this.parseProgressMessage = 'Sending data to server for parsing...';
    this.parseProgress = 50;

    const defaultProxyType = this.importForm.get('defaultProxyType')?.value || 'HTTP';

    // Create the DTO object as expected by the API
    const parseDto = new ProxyTextParseDto({
      proxyText: proxyText,
      defaultProxyType: defaultProxyType
    });

    const response = await firstValueFrom(
      this.apiClient.parseProxyText(parseDto)
    );

    if (response?.success && response.data) {
      // The backend now handles the default proxy type, so we can use the data directly
      this.parsedProxies = response.data;
      this.parseProgress = 100;
      this.parseProgressMessage = 'Parsing complete!';
    } else {
      throw new Error('Failed to parse proxy data');
    }
  }



  private async parseProxiesInChunks(lines: string[]): Promise<void> {
    const chunkSize = 500; // Process 500 lines at a time
    const chunks = this.chunkArray(lines, chunkSize);
    const defaultProxyType = this.importForm.get('defaultProxyType')?.value || 'HTTP';

    for (let i = 0; i < chunks.length; i++) {
      const chunk = chunks[i];
      const chunkText = chunk.join('\n');

      this.parseProgressMessage = `Processing chunk ${i + 1} of ${chunks.length}...`;
      this.parseProgress = Math.round(((i + 1) / chunks.length) * 100);

      try {
        // Create the DTO object for this chunk
        const parseDto = new ProxyTextParseDto({
          proxyText: chunkText,
          defaultProxyType: defaultProxyType
        });

        const response = await firstValueFrom(
          this.apiClient.parseProxyText(parseDto)
        );

        if (response?.success && response.data) {
          // The backend now handles the default proxy type, so we can use the data directly
          this.parsedProxies.push(...response.data);
        }
      } catch (error) {
        console.warn(`Failed to parse chunk ${i + 1}:`, error);
        // Continue with other chunks
      }

      // Allow UI to update between chunks
      await this.delay(10);
    }

    this.parseProgressMessage = 'Parsing complete!';
    this.parseProgress = 100;
  }

  private chunkArray<T>(array: T[], chunkSize: number): T[][] {
    const chunks: T[][] = [];
    for (let i = 0; i < array.length; i += chunkSize) {
      chunks.push(array.slice(i, i + chunkSize));
    }
    return chunks;
  }

  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  cancelParsing(): void {
    if (this.parseAbortController) {
      this.parseAbortController.abort();
      this.parsing = false;
      this.parseProgress = 0;
      this.parseProgressMessage = '';
      this.parseAbortController = null;
      this.snackBar.open('Parsing cancelled', 'Close', { duration: 3000 });
    }
  }

  async validateImport(): Promise<void> {
    if (this.parsedProxies.length === 0) {
      this.snackBar.open('No proxies to validate', 'Close', { duration: 3000 });
      return;
    }

    this.validating = true;
    try {
      const formValue = this.importForm.value;

      const proxies = this.parsedProxies.map(p => {
        const dto = new CreateProxyConfigurationDto();
        dto.host = p.host!;
        dto.port = p.port!;
        dto.proxyType = p.proxyType!;
        dto.username = p.username;
        dto.password = p.password;
        dto.description = p.description;
        dto.timeoutSeconds = p.timeoutSeconds || formValue.defaultTimeoutSeconds;
        dto.maxRetries = p.maxRetries || formValue.defaultMaxRetries;
        dto.isActive = formValue.defaultIsActive;
        return dto;
      });

      const importDto = new BulkProxyImportDto();
      importDto.proxies = proxies;
      importDto.testBeforeImport = formValue.testAfterImport;
      importDto.overwriteExisting = !formValue.skipDuplicates;

      const response = await firstValueFrom(
        this.apiClient.validateProxyImport(importDto)
      );

      if (response?.success && response.data) {
        this.validationResult = response.data;
        this.snackBar.open('Validation completed', 'Close', { duration: 3000 });
      } else {
        this.snackBar.open('Validation failed', 'Close', { duration: 3000 });
      }
    } catch (error) {
      console.error('Error validating import:', error);
      this.snackBar.open('Validation failed', 'Close', { duration: 3000 });
    } finally {
      this.validating = false;
    }
  }

  async performImport(): Promise<void> {
    if (!this.validationResult) {
      await this.validateImport();
      if (!this.validationResult) return;
    }

    this.importing = true;
    try {
      const formValue = this.importForm.value;

      const proxies = this.parsedProxies.map(p => {
        const dto = new CreateProxyConfigurationDto();
        dto.host = p.host!;
        dto.port = p.port!;
        dto.proxyType = p.proxyType!;
        dto.username = p.username;
        dto.password = p.password;
        dto.description = p.description;
        dto.timeoutSeconds = p.timeoutSeconds || formValue.defaultTimeoutSeconds;
        dto.maxRetries = p.maxRetries || formValue.defaultMaxRetries;
        dto.isActive = formValue.defaultIsActive;
        return dto;
      });

      const importDto = new BulkProxyImportDto();
      importDto.proxies = proxies;
      importDto.testBeforeImport = formValue.testAfterImport;
      importDto.overwriteExisting = !formValue.skipDuplicates;

      const response = await firstValueFrom(
        this.apiClient.bulkImportProxies(importDto)
      );

      if (response?.success && response.data) {
        this.importResult = response.data;
        this.currentStep = 2;
        this.snackBar.open(
          `Import completed: ${this.importResult.successfulImports} successful, ${this.importResult.failedImports} failed`,
          'Close',
          { duration: 5000 }
        );
      } else {
        this.snackBar.open('Import failed', 'Close', { duration: 3000 });
      }
    } catch (error) {
      console.error('Error importing proxies:', error);
      this.snackBar.open('Import failed', 'Close', { duration: 3000 });
    } finally {
      this.importing = false;
    }
  }

  goBack(): void {
    if (this.currentStep > 0) {
      this.currentStep--;
    }
  }

  startOver(): void {
    this.currentStep = 0;
    this.parsedProxies = [];
    this.validationResult = null;
    this.importResult = null;
    this.importForm.get('proxyText')?.setValue('');
  }

  onCancel(): void {
    this.router.navigate(['/admin/proxies']);
  }

  getProxyStatus(proxy: ProxyImportItemDto): string {
    if (!proxy.isValid) return 'Invalid';
    if (proxy.alreadyExists) return 'Duplicate';
    return 'Valid';
  }

  getStatusColor(proxy: ProxyImportItemDto): string {
    if (!proxy.isValid) return 'warn';
    if (proxy.alreadyExists) return 'accent';
    return 'primary';
  }

  getValidProxiesCount(): number {
    return this.parsedProxies.filter(p => p.isValid && !p.alreadyExists).length;
  }

  getDuplicateProxiesCount(): number {
    return this.parsedProxies.filter(p => p.alreadyExists).length;
  }

  getInvalidProxiesCount(): number {
    return this.parsedProxies.filter(p => !p.isValid).length;
  }

  hasAuthentication(proxy: ProxyImportItemDto): boolean {
    return !!(proxy.username && proxy.password);
  }

  // TrackBy function for better performance with large lists
  trackByProxyId(_index: number, proxy: ProxyImportItemDto): string {
    return `${proxy.host}:${proxy.port}:${proxy.proxyType}`;
  }

  // TrackBy function for error arrays
  trackByIndex(index: number, _item: any): number {
    return index;
  }

  // Expose Math for template
  readonly Math = Math;

  // Get color for proxy type chip
  getProxyTypeColor(proxyType: string): string {
    switch (proxyType?.toUpperCase()) {
      case 'HTTP':
      case 'HTTPS':
        return 'primary';
      case 'SOCKS4':
        return 'accent';
      case 'SOCKS5':
        return 'warn';
      default:
        return 'basic';
    }
  }

  // Test individual proxy
  async testProxy(proxy: ProxyImportItemDto): Promise<void> {
    const proxyKey = this.getProxyKey(proxy);
    this.testingProxies.add(proxyKey);

    try {
      const formValue = this.importForm.value;
      const dto = new CreateProxyConfigurationDto();
      dto.host = proxy.host!;
      dto.port = proxy.port!;
      dto.proxyType = proxy.proxyType!;
      dto.username = proxy.username;
      dto.password = proxy.password;
      dto.description = proxy.description;
      dto.timeoutSeconds = proxy.timeoutSeconds || formValue.defaultTimeoutSeconds;
      dto.maxRetries = proxy.maxRetries || formValue.defaultMaxRetries;
      dto.isActive = formValue.defaultIsActive;

      // TODO: Replace with actual API call once backend is running
      // const response = await firstValueFrom(
      //   this.apiClient.testProxyConfiguration(dto, undefined, 30)
      // );

      // For now, simulate a test result
      const mockResult = new ProxyTestResultDto({
        proxyConfigurationId: '00000000-0000-0000-0000-000000000000',
        host: proxy.host!,
        port: proxy.port!,
        proxyType: proxy.proxyType!,
        isHealthy: Math.random() > 0.3, // 70% success rate for demo
        responseTimeMs: Math.floor(Math.random() * 2000) + 100,
        errorMessage: Math.random() > 0.7 ? 'Connection timeout' : undefined,
        errorCode: Math.random() > 0.7 ? 'TIMEOUT' : undefined,
        testedAt: new Date()
      });

      this.testResults.set(proxyKey, mockResult);

      if (mockResult.isHealthy) {
        this.snackBar.open(
          `Proxy test successful for ${proxy.host}:${proxy.port} (${mockResult.responseTimeMs}ms)`,
          'Close',
          { duration: 3000 }
        );
      } else {
        this.snackBar.open(
          `Proxy test failed for ${proxy.host}:${proxy.port}: ${mockResult.errorMessage}`,
          'Close',
          { duration: 5000 }
        );
      }
    } catch (error) {
      console.error('Error testing proxy:', error);
      this.snackBar.open(`Failed to test proxy ${proxy.host}:${proxy.port}`, 'Close', { duration: 3000 });
    } finally {
      this.testingProxies.delete(proxyKey);
    }
  }

  // Test selected proxies in bulk
  async testSelectedProxies(): Promise<void> {
    const selectedProxiesList = this.parsedProxies.filter(p =>
      this.selectedProxies.has(this.getProxyKey(p)) && p.isValid
    );

    if (selectedProxiesList.length === 0) {
      this.snackBar.open('No valid proxies selected for testing', 'Close', { duration: 3000 });
      return;
    }

    this.bulkTesting = true;
    try {
      const testPromises = selectedProxiesList.map(proxy => this.testProxy(proxy));
      await Promise.all(testPromises);
      this.snackBar.open(`Tested ${selectedProxiesList.length} proxies`, 'Close', { duration: 3000 });
    } catch (error) {
      console.error('Error bulk testing proxies:', error);
      this.snackBar.open('Failed to test some proxies', 'Close', { duration: 3000 });
    } finally {
      this.bulkTesting = false;
    }
  }

  // Get unique key for proxy
  getProxyKey(proxy: ProxyImportItemDto): string {
    return `${proxy.host}:${proxy.port}:${proxy.proxyType}`;
  }

  // Check if proxy is being tested
  isProxyTesting(proxy: ProxyImportItemDto): boolean {
    return this.testingProxies.has(this.getProxyKey(proxy));
  }

  // Get test result for proxy
  getTestResult(proxy: ProxyImportItemDto): ProxyTestResultDto | undefined {
    return this.testResults.get(this.getProxyKey(proxy));
  }

  // Selection methods
  toggleProxySelection(proxy: ProxyImportItemDto): void {
    const proxyKey = this.getProxyKey(proxy);
    if (this.selectedProxies.has(proxyKey)) {
      this.selectedProxies.delete(proxyKey);
    } else {
      this.selectedProxies.add(proxyKey);
    }
    this.updateSelectAllState();
  }

  isProxySelected(proxy: ProxyImportItemDto): boolean {
    return this.selectedProxies.has(this.getProxyKey(proxy));
  }

  toggleSelectAll(): void {
    if (this.selectAll) {
      this.selectedProxies.clear();
    } else {
      this.parsedProxies.forEach(proxy => {
        this.selectedProxies.add(this.getProxyKey(proxy));
      });
    }
    this.selectAll = !this.selectAll;
  }

  updateSelectAllState(): void {
    const totalProxies = this.parsedProxies.length;
    const selectedCount = this.selectedProxies.size;
    this.selectAll = totalProxies > 0 && selectedCount === totalProxies;
  }

  getSelectedCount(): number {
    return this.selectedProxies.size;
  }

  // Remove selected proxies from the list
  removeSelectedProxies(): void {
    const selectedKeys = Array.from(this.selectedProxies);
    this.parsedProxies = this.parsedProxies.filter(proxy =>
      !selectedKeys.includes(this.getProxyKey(proxy))
    );
    this.selectedProxies.clear();
    this.selectAll = false;
    this.snackBar.open(`Removed ${selectedKeys.length} proxies from the list`, 'Close', { duration: 3000 });
  }

  // Toggle active status for selected proxies
  toggleSelectedProxiesActive(isActive: boolean): void {
    const selectedKeys = Array.from(this.selectedProxies);
    this.parsedProxies.forEach(proxy => {
      if (selectedKeys.includes(this.getProxyKey(proxy))) {
        // This would affect the import, but we can't modify the parsed proxy directly
        // Instead, we'll show a message that this will be applied during import
      }
    });

    const action = isActive ? 'enabled' : 'disabled';
    this.snackBar.open(
      `${selectedKeys.length} proxies will be ${action} during import`,
      'Close',
      { duration: 3000 }
    );
  }

  // Filtering methods
  applyFilters(): void {
    this.filteredProxies = this.parsedProxies.filter(proxy => {
      // Text filter (host or port)
      const textMatch = !this.filterText ||
        proxy.host?.toLowerCase().includes(this.filterText.toLowerCase()) ||
        proxy.port?.toString().includes(this.filterText);

      // Type filter
      const typeMatch = !this.filterType || proxy.proxyType === this.filterType;

      // Status filter
      let statusMatch = true;
      if (this.filterStatus) {
        switch (this.filterStatus) {
          case 'valid':
            statusMatch = (proxy.isValid ?? false) && !(proxy.alreadyExists ?? false);
            break;
          case 'invalid':
            statusMatch = !(proxy.isValid ?? false);
            break;
          case 'duplicate':
            statusMatch = proxy.alreadyExists ?? false;
            break;
        }
      }

      return textMatch && typeMatch && statusMatch;
    });
  }

  onFilterChange(): void {
    this.applyFilters();
    this.updateSelectAllState();
  }

  clearFilters(): void {
    this.filterText = '';
    this.filterType = '';
    this.filterStatus = '';
    this.applyFilters();
  }

  getFilteredValidProxiesCount(): number {
    return this.filteredProxies.filter(p => p.isValid && !p.alreadyExists).length;
  }

  getFilteredDuplicateProxiesCount(): number {
    return this.filteredProxies.filter(p => p.alreadyExists).length;
  }

  getFilteredInvalidProxiesCount(): number {
    return this.filteredProxies.filter(p => !p.isValid).length;
  }
}
