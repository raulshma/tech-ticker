import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { firstValueFrom } from 'rxjs';
import {
  ProxyConfigurationDto,
  CreateProxyConfigurationDto,
  UpdateProxyConfigurationDto,
  TechTickerApiClient
} from '../../../shared/api/api-client';

@Component({
  selector: 'app-proxy-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatSlideToggleModule,
    MatExpansionModule,
    MatChipsModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './proxy-form.component.html',
  styleUrls: ['./proxy-form.component.scss']
})
export class ProxyFormComponent implements OnInit {
  proxyForm: FormGroup;
  loading = false;
  isEditMode = false;
  proxyId: string | null = null;

  // UI state
  hidePassword = true;
  hasError = false;
  errorMessage = '';

  proxyTypes = [
    { value: 'HTTP', label: 'HTTP' },
    { value: 'HTTPS', label: 'HTTPS' },
    { value: 'SOCKS4', label: 'SOCKS4' },
    { value: 'SOCKS5', label: 'SOCKS5' }
  ];

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar,
    private apiClient: TechTickerApiClient
  ) {
    this.proxyForm = this.createForm();
  }

  ngOnInit(): void {
    this.proxyId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.proxyId;

    if (this.isEditMode && this.proxyId) {
      this.loadProxy(this.proxyId);
    }
  }

  private createForm(): FormGroup {
    return this.fb.group({
      host: ['', [Validators.required, Validators.maxLength(255)]],
      port: [8080, [Validators.required, Validators.min(1), Validators.max(65535)]],
      proxyType: ['HTTP', [Validators.required]],
      username: ['', [Validators.maxLength(100)]],
      password: ['', [Validators.maxLength(500)]],
      description: ['', [Validators.maxLength(200)]],
      timeoutSeconds: [30, [Validators.required, Validators.min(1), Validators.max(300)]],
      maxRetries: [3, [Validators.required, Validators.min(0), Validators.max(10)]],
      isActive: [true]
    });
  }

  // Helper method to get proxy type icon
  getProxyTypeIcon(proxyType: string | undefined): string {
    switch (proxyType?.toUpperCase()) {
      case 'HTTP':
      case 'HTTPS':
        return 'language';
      case 'SOCKS4':
        return 'hub';
      case 'SOCKS5':
        return 'security';
      default:
        return 'cloud';
    }
  }

  // Check if connection test is available
  canTestConnection(): boolean {
    return !!(this.proxyForm.get('host')?.valid && 
           this.proxyForm.get('port')?.valid && 
           this.proxyForm.get('proxyType')?.valid);
  }

  // Enhanced error handling
  private handleError(error: any, operation: string): void {
    console.error(`${operation} failed:`, error);
    this.hasError = true;
    this.errorMessage = `Failed to ${operation.toLowerCase()}. Please check your configuration and try again.`;
    this.snackBar.open(this.errorMessage, 'Retry', { 
      duration: 5000 
    }).onAction().subscribe(() => {
      this.retryLastOperation();
    });
  }

  retryLastOperation(): void {
    this.hasError = false;
    this.errorMessage = '';
    
    if (this.isEditMode && this.proxyId) {
      this.loadProxy(this.proxyId);
    }
  }

  async loadProxy(id: string): Promise<void> {
    this.loading = true;
    this.hasError = false;
    try {
      const response = await firstValueFrom(this.apiClient.getProxyById(id));
      if (response?.success && response.data) {
        const proxy = response.data;
        this.proxyForm.patchValue({
          host: proxy.host,
          port: proxy.port,
          proxyType: proxy.proxyType,
          username: proxy.username,
          password: '', // Don't populate password for security
          description: proxy.description,
          timeoutSeconds: proxy.timeoutSeconds,
          maxRetries: proxy.maxRetries,
          isActive: proxy.isActive
        });
      } else {
        this.handleError(new Error('Proxy not found'), 'Load proxy');
        this.router.navigate(['/admin/proxies']);
      }
    } catch (error) {
      this.handleError(error, 'Load proxy');
      this.router.navigate(['/admin/proxies']);
    } finally {
      this.loading = false;
    }
  }

  async onSubmit(): Promise<void> {
    if (this.proxyForm.valid) {
      this.loading = true;
      this.hasError = false;
      try {
        const formValue = this.proxyForm.value;

        if (this.isEditMode && this.proxyId) {
          // Update existing proxy
          const updateDto = new UpdateProxyConfigurationDto();
          updateDto.host = formValue.host;
          updateDto.port = formValue.port;
          updateDto.proxyType = formValue.proxyType;
          updateDto.username = formValue.username || undefined;
          updateDto.password = formValue.password || undefined;
          updateDto.description = formValue.description || undefined;
          updateDto.timeoutSeconds = formValue.timeoutSeconds;
          updateDto.maxRetries = formValue.maxRetries;
          updateDto.isActive = formValue.isActive;

          const response = await firstValueFrom(
            this.apiClient.updateProxy(this.proxyId, updateDto)
          );

          if (response?.success) {
            this.snackBar.open('Proxy updated successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/admin/proxies']);
          } else {
            this.handleError(new Error(response?.message || 'Update failed'), 'Update proxy');
          }
        } else {
          // Create new proxy
          const createDto = new CreateProxyConfigurationDto();
          createDto.host = formValue.host;
          createDto.port = formValue.port;
          createDto.proxyType = formValue.proxyType;
          createDto.username = formValue.username || undefined;
          createDto.password = formValue.password || undefined;
          createDto.description = formValue.description || undefined;
          createDto.timeoutSeconds = formValue.timeoutSeconds;
          createDto.maxRetries = formValue.maxRetries;
          createDto.isActive = formValue.isActive;

          const response = await firstValueFrom(
            this.apiClient.createProxy(createDto)
          );

          if (response?.success) {
            this.snackBar.open('Proxy created successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/admin/proxies']);
          } else {
            this.handleError(new Error(response?.message || 'Create failed'), 'Create proxy');
          }
        }
      } catch (error) {
        this.handleError(error, this.isEditMode ? 'Update proxy' : 'Create proxy');
      } finally {
        this.loading = false;
      }
    } else {
      this.markFormGroupTouched();
      this.snackBar.open('Please correct the form errors before submitting', 'Close', { duration: 3000 });
    }
  }

  onCancel(): void {
    this.router.navigate(['/admin/proxies']);
  }

  async testConnection(): Promise<void> {
    if (!this.canTestConnection()) {
      this.snackBar.open('Please fill in host, port, and proxy type before testing', 'Close', { duration: 3000 });
      return;
    }

    if (!this.isEditMode) {
      this.snackBar.open('Please save the proxy first to test the connection', 'Close', { duration: 3000 });
      return;
    }

    this.loading = true;
    try {
      if (this.proxyId) {
        const response = await firstValueFrom(
          this.apiClient.testProxy(this.proxyId, undefined, 30)
        );

        if (response?.success && response.data) {
          const result = response.data;
          if (result.isHealthy) {
            this.snackBar.open(
              `Connection test successful! Response time: ${result.responseTimeMs}ms`,
              'Close',
              { duration: 5000 }
            );
          } else {
            this.snackBar.open(
              `Connection test failed: ${result.errorMessage}`,
              'Close',
              { duration: 5000 }
            );
          }
        } else {
          this.snackBar.open('Connection test completed with unknown result', 'Close', { duration: 3000 });
        }
      }
    } catch (error) {
      console.error('Error testing connection:', error);
      this.snackBar.open('Failed to test connection', 'Close', { duration: 3000 });
    } finally {
      this.loading = false;
    }
  }

  private markFormGroupTouched(): void {
    Object.keys(this.proxyForm.controls).forEach(key => {
      const control = this.proxyForm.get(key);
      control?.markAsTouched();
    });
  }

  getFieldError(fieldName: string): string {
    const control = this.proxyForm.get(fieldName);
    if (control && control.errors && control.touched) {
      const errors = control.errors;
      
      if (errors['required']) return `${fieldName} is required`;
      if (errors['maxlength']) return `${fieldName} is too long (max ${errors['maxlength'].requiredLength} characters)`;
      if (errors['min']) return `${fieldName} must be at least ${errors['min'].min}`;
      if (errors['max']) return `${fieldName} must be at most ${errors['max'].max}`;
      if (errors['email']) return `${fieldName} must be a valid email`;
      
      return `${fieldName} is invalid`;
    }
    return '';
  }
}
