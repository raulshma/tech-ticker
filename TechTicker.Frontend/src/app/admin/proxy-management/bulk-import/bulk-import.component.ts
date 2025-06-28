import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
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
import { ScrollingModule } from '@angular/cdk/scrolling';
import { firstValueFrom, interval, take } from 'rxjs';
import {
  ProxyImportItemDto,
  BulkProxyImportDto,
  BulkProxyImportValidationDto,
  BulkProxyImportResultDto,
  CreateProxyConfigurationDto,
  TechTickerApiClient
} from '../../../shared/api/api-client';

@Component({
  selector: 'app-bulk-import',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
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

  displayedColumns: string[] = ['host', 'port', 'type', 'auth', 'status', 'errors'];

  currentStep = 0; // 0: Input, 1: Preview, 2: Results

  // Virtual scrolling
  readonly itemSize = 48; // Height of each row in pixels

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

    const response = await firstValueFrom(
      this.apiClient.parseProxyText(proxyText)
    );

    if (response?.success && response.data) {
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

    for (let i = 0; i < chunks.length; i++) {
      const chunk = chunks[i];
      const chunkText = chunk.join('\n');

      this.parseProgressMessage = `Processing chunk ${i + 1} of ${chunks.length}...`;
      this.parseProgress = Math.round(((i + 1) / chunks.length) * 100);

      try {
        const response = await firstValueFrom(
          this.apiClient.parseProxyText(chunkText)
        );

        if (response?.success && response.data) {
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
}
