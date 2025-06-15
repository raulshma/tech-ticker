import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { finalize } from 'rxjs/operators';
import { SiteConfiguration, SiteConfigurationService, CreateSiteConfigurationDto, UpdateSiteConfigurationDto } from '../../services/site-configuration.service';

@Component({
  selector: 'app-site-config-form',
  templateUrl: './site-config-form.component.html',
  styleUrls: ['./site-config-form.component.css']
})
export class SiteConfigFormComponent implements OnInit {
  form!: FormGroup;
  loading = false;
  isEdit = false;
  siteConfigId: string | null = null;
  testingSelectors = false;
  testUrl = '';
  testResults: any = null;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private siteConfigService: SiteConfigurationService,
    private message: NzMessageService
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.siteConfigId = params['id'];
        this.isEdit = true;
        this.loadSiteConfig();
      }
    });
  }

  initForm(): void {
    this.form = this.fb.group({
      domain: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$/)]],
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(500)]],
      isActive: [true],
      selectors: this.fb.group({
        productName: [''],
        price: [''],
        availability: [''],
        image: [''],
        description: ['']
      })
    });
  }

  loadSiteConfig(): void {
    if (!this.siteConfigId) return;

    this.loading = true;
    this.siteConfigService.getSiteConfigurationById(this.siteConfigId)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (siteConfig) => {
          this.form.patchValue({
            domain: siteConfig.domain,
            name: siteConfig.name,
            description: siteConfig.description,
            isActive: siteConfig.isActive,
            selectors: siteConfig.selectors
          });
        },
        error: (error) => {
          console.error('Error loading site configuration:', error);
          this.message.error('Failed to load site configuration');
          this.router.navigate(['/mappings/site-configs']);
        }
      });
  }

  onSubmit(): void {
    if (this.form.valid) {
      this.loading = true;

      const formValue = this.form.value;

      if (this.isEdit && this.siteConfigId) {
        const updateData: UpdateSiteConfigurationDto = {
          domain: formValue.domain,
          name: formValue.name,
          description: formValue.description,
          isActive: formValue.isActive,
          selectors: formValue.selectors
        };

        this.siteConfigService.updateSiteConfiguration(this.siteConfigId, updateData)
          .pipe(finalize(() => this.loading = false))
          .subscribe({
            next: () => {
              this.message.success('Site configuration updated successfully');
              this.router.navigate(['/mappings/site-configs']);
            },
            error: (error) => {
              console.error('Error updating site configuration:', error);
              this.message.error('Failed to update site configuration');
            }
          });
      } else {
        const createData: CreateSiteConfigurationDto = {
          domain: formValue.domain,
          name: formValue.name,
          description: formValue.description,
          isActive: formValue.isActive,
          selectors: formValue.selectors
        };

        this.siteConfigService.createSiteConfiguration(createData)
          .pipe(finalize(() => this.loading = false))
          .subscribe({
            next: () => {
              this.message.success('Site configuration created successfully');
              this.router.navigate(['/mappings/site-configs']);
            },
            error: (error) => {
              console.error('Error creating site configuration:', error);
              this.message.error('Failed to create site configuration');
            }
          });
      }
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel(): void {
    this.router.navigate(['/mappings/site-configs']);
  }

  testSelectors(): void {
    if (!this.testUrl || !this.form.value.domain) {
      this.message.warning('Please enter a test URL and domain');
      return;
    }

    this.testingSelectors = true;
    this.siteConfigService.testSelectors(
      this.form.value.domain,
      this.form.value.selectors,
      this.testUrl
    )
    .pipe(finalize(() => this.testingSelectors = false))
    .subscribe({
      next: (results) => {
        this.testResults = results;
        if (results.success) {
          this.message.success('Selectors tested successfully');
        } else {
          this.message.warning('Some selectors may need adjustment');
        }
      },
      error: (error) => {
        console.error('Error testing selectors:', error);
        this.message.error('Failed to test selectors');
        this.testResults = null;
      }
    });
  }

  clearTestResults(): void {
    this.testResults = null;
    this.testUrl = '';
  }

  private markFormGroupTouched(): void {
    Object.keys(this.form.controls).forEach(key => {
      const control = this.form.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup) {
        Object.keys(control.controls).forEach(nestedKey => {
          control.get(nestedKey)?.markAsTouched();
        });
      }
    });
  }

  getFieldError(fieldName: string): string | null {
    const field = this.form.get(fieldName);
    if (field && field.invalid && field.touched) {
      if (field.errors?.['required']) {
        return `${this.getFieldLabel(fieldName)} is required`;
      }
      if (field.errors?.['pattern']) {
        return `${this.getFieldLabel(fieldName)} format is invalid`;
      }
      if (field.errors?.['minlength']) {
        return `${this.getFieldLabel(fieldName)} must be at least ${field.errors['minlength'].requiredLength} characters`;
      }
      if (field.errors?.['maxlength']) {
        return `${this.getFieldLabel(fieldName)} must not exceed ${field.errors['maxlength'].requiredLength} characters`;
      }
    }
    return null;
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      domain: 'Domain',
      name: 'Name',
      description: 'Description'
    };
    return labels[fieldName] || fieldName;
  }

  getSelectorFieldError(selectorName: string): string | null {
    const field = this.form.get(`selectors.${selectorName}`);
    if (field && field.invalid && field.touched) {
      return `Invalid CSS selector`;
    }
    return null;
  }
}
