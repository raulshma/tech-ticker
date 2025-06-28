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
import { firstValueFrom } from 'rxjs';
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
    MatTabsModule
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

  parsedProxies: ProxyImportItemDto[] = [];
  validationResult: BulkProxyImportValidationDto | null = null;
  importResult: BulkProxyImportResultDto | null = null;

  displayedColumns: string[] = ['host', 'port', 'type', 'auth', 'status', 'errors'];

  currentStep = 0; // 0: Input, 1: Preview, 2: Results

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

    this.parsing = true;
    try {
      const response = await firstValueFrom(
        this.apiClient.parseProxyText(this.importForm.get('proxyText')?.value)
      );

      if (response?.success && response.data) {
        this.parsedProxies = response.data;
        this.currentStep = 1;
        this.snackBar.open(`Parsed ${this.parsedProxies.length} proxies`, 'Close', { duration: 3000 });
      } else {
        this.snackBar.open('Failed to parse proxy data', 'Close', { duration: 3000 });
      }
    } catch (error) {
      console.error('Error parsing proxies:', error);
      this.snackBar.open('Failed to parse proxy data', 'Close', { duration: 3000 });
    } finally {
      this.parsing = false;
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
}
