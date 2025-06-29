import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormArray, AbstractControl } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  ScraperSiteConfigurationDto,
  CreateScraperSiteConfigurationDto,
  UpdateScraperSiteConfigurationDto
} from '../../../../shared/api/api-client';
import { SiteConfigsService } from '../../services/site-configs.service';

interface HeaderEntry {
  key: string;
  value: string;
}

@Component({
  selector: 'app-site-config-form',
  templateUrl: './site-config-form.component.html',
  styleUrls: ['./site-config-form.component.scss'],
  standalone: false
})
export class SiteConfigFormComponent implements OnInit {
  siteConfigForm: FormGroup;
  isLoading = false;
  isEditMode = false;
  siteConfigId: string | null = null;

  constructor(
    private formBuilder: FormBuilder,
    private siteConfigsService: SiteConfigsService,
    private router: Router,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar
  ) {
    this.siteConfigForm = this.formBuilder.group({
      siteDomain: ['', [Validators.required, Validators.maxLength(200)]],
      productNameSelector: ['', [Validators.maxLength(500)]],
      priceSelector: ['', [Validators.maxLength(500)]],
      stockSelector: ['', [Validators.maxLength(500)]],
      sellerNameOnPageSelector: ['', [Validators.maxLength(500)]],
      imageSelector: ['', [Validators.maxLength(500)]],
      defaultUserAgent: ['', [Validators.maxLength(1000)]],
      additionalHeaders: this.formBuilder.array([]),
      isEnabled: [true],
      requiresBrowserAutomation: [false],
      browserAutomationProfile: [''],
    });
  }

  ngOnInit(): void {
    this.siteConfigId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.siteConfigId;

    if (this.isEditMode && this.siteConfigId) {
      this.loadSiteConfig(this.siteConfigId);
    } else {
      // Add default user agent for new configs
      this.siteConfigForm.patchValue({
        defaultUserAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
      });
    }
  }

  get additionalHeaders(): FormArray {
    return this.siteConfigForm.get('additionalHeaders') as FormArray;
  }

  loadSiteConfig(id: string): void {
    this.isLoading = true;
    this.siteConfigsService.getSiteConfig(id).subscribe({
      next: (config) => {
        const configAny = config as any;
        this.siteConfigForm.patchValue({
          siteDomain: config.siteDomain,
          productNameSelector: config.productNameSelector,
          priceSelector: config.priceSelector,
          stockSelector: config.stockSelector,
          sellerNameOnPageSelector: config.sellerNameOnPageSelector,
          imageSelector: configAny.imageSelector,
          defaultUserAgent: config.defaultUserAgent,
          isEnabled: config.isEnabled,
          requiresBrowserAutomation: configAny.requiresBrowserAutomation ?? false,
          browserAutomationProfile: configAny.browserAutomationProfile ?? ''
        });

        // Load additional headers
        if (config.additionalHeaders) {
          Object.entries(config.additionalHeaders).forEach(([key, value]) => {
            this.addHeader(key, value);
          });
        }

        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading site configuration:', error);
        this.snackBar.open('Failed to load site configuration', 'Close', { duration: 5000 });
        this.router.navigate(['/site-configs']);
      }
    });
  }

  addHeader(key: string = '', value: string = ''): void {
    const headerGroup = this.formBuilder.group({
      key: [key, [Validators.required]],
      value: [value, [Validators.required]]
    });
    this.additionalHeaders.push(headerGroup);
  }

  removeHeader(index: number): void {
    this.additionalHeaders.removeAt(index);
  }

  onSubmit(): void {
    if (this.siteConfigForm.valid && !this.isLoading) {
      this.isLoading = true;

      const formValue = this.siteConfigForm.value;

      // Convert headers array to object
      const headersObject: { [key: string]: string } = {};
      formValue.additionalHeaders.forEach((header: HeaderEntry) => {
        if (header.key && header.value) {
          headersObject[header.key] = header.value;
        }
      });

      if (this.isEditMode && this.siteConfigId) {
        const updateDto = new UpdateScraperSiteConfigurationDto({
          siteDomain: formValue.siteDomain,
          productNameSelector: formValue.productNameSelector || undefined,
          priceSelector: formValue.priceSelector || undefined,
          stockSelector: formValue.stockSelector || undefined,
          sellerNameOnPageSelector: formValue.sellerNameOnPageSelector || undefined,
          defaultUserAgent: formValue.defaultUserAgent || undefined,
          additionalHeaders: Object.keys(headersObject).length > 0 ? headersObject : undefined,
          isEnabled: formValue.isEnabled,
          requiresBrowserAutomation: formValue.requiresBrowserAutomation,
          browserAutomationProfile: formValue.browserAutomationProfile || undefined
        });

        // Add image selector using type assertion until API client is regenerated
        (updateDto as any).imageSelector = formValue.imageSelector || undefined;

        this.siteConfigsService.updateSiteConfig(this.siteConfigId, updateDto).subscribe({
          next: () => {
            this.snackBar.open('Site configuration updated successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/site-configs']);
          },
          error: (error) => {
            console.error('Error updating site configuration:', error);
            this.snackBar.open('Failed to update site configuration', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      } else {
        const createDto = new CreateScraperSiteConfigurationDto({
          siteDomain: formValue.siteDomain,
          productNameSelector: formValue.productNameSelector || undefined,
          priceSelector: formValue.priceSelector || undefined,
          stockSelector: formValue.stockSelector || undefined,
          sellerNameOnPageSelector: formValue.sellerNameOnPageSelector || undefined,
          defaultUserAgent: formValue.defaultUserAgent || undefined,
          additionalHeaders: Object.keys(headersObject).length > 0 ? headersObject : undefined,
          isEnabled: formValue.isEnabled,
          requiresBrowserAutomation: formValue.requiresBrowserAutomation,
          browserAutomationProfile: formValue.browserAutomationProfile || undefined
        });

        // Add image selector using type assertion until API client is regenerated
        (createDto as any).imageSelector = formValue.imageSelector || undefined;

        this.siteConfigsService.createSiteConfig(createDto).subscribe({
          next: () => {
            this.snackBar.open('Site configuration created successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/site-configs']);
          },
          error: (error) => {
            console.error('Error creating site configuration:', error);
            this.snackBar.open('Failed to create site configuration', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      }
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel(): void {
    this.router.navigate(['/site-configs']);
  }

  private markFormGroupTouched(): void {
    Object.keys(this.siteConfigForm.controls).forEach(key => {
      const control = this.siteConfigForm.get(key);
      if (control) {
        control.markAsTouched();
      }
    });

    // Mark header controls as touched
    this.additionalHeaders.controls.forEach(headerGroup => {
      const formGroup = headerGroup as FormGroup;
      Object.keys(formGroup.controls).forEach(key => {
        formGroup.get(key)?.markAsTouched();
      });
    });
  }

  getFieldErrorMessage(fieldName: string): string {
    const control = this.siteConfigForm.get(fieldName);
    if (control?.hasError('required')) {
      return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} is required`;
    }
    if (control?.hasError('maxlength')) {
      const maxLength = control.errors?.['maxlength']?.requiredLength;
      return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} must be less than ${maxLength} characters`;
    }
    return '';
  }
}
