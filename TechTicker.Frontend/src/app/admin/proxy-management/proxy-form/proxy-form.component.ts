import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
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
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatSnackBarModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './proxy-form.component.html',
  styleUrls: ['./proxy-form.component.scss']
})
export class ProxyFormComponent implements OnInit {
  proxyForm: FormGroup;
  loading = false;
  isEditMode = false;
  proxyId: string | null = null;

  proxyTypes = [
    { value: 'HTTP', label: 'HTTP' },
    { value: 'HTTPS', label: 'HTTPS' },
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

  async loadProxy(id: string): Promise<void> {
    this.loading = true;
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
        this.snackBar.open('Proxy not found', 'Close', { duration: 3000 });
        this.router.navigate(['/admin/proxies']);
      }
    } catch (error) {
      console.error('Error loading proxy:', error);
      this.snackBar.open('Failed to load proxy', 'Close', { duration: 3000 });
      this.router.navigate(['/admin/proxies']);
    } finally {
      this.loading = false;
    }
  }

  async onSubmit(): Promise<void> {
    if (this.proxyForm.valid) {
      this.loading = true;
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
            this.snackBar.open('Failed to update proxy', 'Close', { duration: 3000 });
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
            this.snackBar.open('Failed to create proxy', 'Close', { duration: 3000 });
          }
        }
      } catch (error) {
        console.error('Error saving proxy:', error);
        this.snackBar.open('Failed to save proxy', 'Close', { duration: 3000 });
      } finally {
        this.loading = false;
      }
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel(): void {
    this.router.navigate(['/admin/proxies']);
  }

  async testConnection(): Promise<void> {
    if (this.proxyForm.get('host')?.valid && this.proxyForm.get('port')?.valid) {
      this.loading = true;
      try {
        // For testing, we'll create a temporary proxy if in create mode
        if (!this.isEditMode) {
          this.snackBar.open('Save the proxy first to test the connection', 'Close', { duration: 3000 });
          return;
        }

        if (this.proxyId) {
          const response = await firstValueFrom(
            this.apiClient.testProxy(this.proxyId, undefined, 30)
          );

          if (response?.success && response.data) {
            const result = response.data;
            if (result.isHealthy) {
              this.snackBar.open(
                `Connection test successful (${result.responseTimeMs}ms)`,
                'Close',
                { duration: 3000 }
              );
            } else {
              this.snackBar.open(
                `Connection test failed: ${result.errorMessage}`,
                'Close',
                { duration: 5000 }
              );
            }
          }
        }
      } catch (error) {
        console.error('Error testing connection:', error);
        this.snackBar.open('Failed to test connection', 'Close', { duration: 3000 });
      } finally {
        this.loading = false;
      }
    } else {
      this.snackBar.open('Please enter valid host and port', 'Close', { duration: 3000 });
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
    if (control?.errors && control.touched) {
      if (control.errors['required']) return `${fieldName} is required`;
      if (control.errors['maxlength']) return `${fieldName} is too long`;
      if (control.errors['min']) return `${fieldName} must be greater than ${control.errors['min'].min}`;
      if (control.errors['max']) return `${fieldName} must be less than ${control.errors['max'].max}`;
    }
    return '';
  }
}
