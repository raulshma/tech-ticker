import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormArray, AbstractControl } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import {
  ScraperSiteConfigurationDto,
  CreateScraperSiteConfigurationDto,
  UpdateScraperSiteConfigurationDto
} from '../../../../shared/api/api-client';
import { SiteConfigsService } from '../../services/site-configs.service';
import { BrowserAutomationProfileDialogComponent } from '../../../../shared/components/browser-automation-profile-builder/browser-automation-profile-dialog.component';

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
  siteConfigId?: string;
  showExamples = false;
  error: string | null = null;

  browserAutomationExamples = [
    {
      title: 'Scroll Down',
      description: 'Scrolls down the page by one viewport.',
      json: `{
  "preferredBrowser": "chromium",
  "timeoutSeconds": 30,
  "actions": [
    { "actionType": "scroll" }
  ]
}`
    },
    {
      title: 'Click Button',
      description: 'Clicks a button with a specific selector.',
      json: `{
  "actions": [
    { "actionType": "click", "selector": ".buy-now-btn" }
  ]
}`
    },
    {
      title: 'Wait for Selector',
      description: 'Waits for a selector to appear before continuing.',
      json: `{
  "actions": [
    { "actionType": "waitForSelector", "selector": ".price-loaded" }
  ]
}`
    },
    {
      title: 'Type in Input',
      description: 'Types text into an input field.',
      json: `{
  "actions": [
    { "actionType": "type", "selector": "#search", "value": "laptop" }
  ]
}`
    },
    {
      title: 'Wait (Timeout)',
      description: 'Waits for a specified number of milliseconds.',
      json: `{
  "actions": [
    { "actionType": "wait", "delayMs": 2000 }
  ]
}`
    },
    {
      title: 'Evaluate JavaScript',
      description: 'Runs custom JavaScript in the page context.',
      json: `{
  "actions": [
    { "actionType": "evaluate", "value": "window.scrollTo(0, document.body.scrollHeight);" }
  ]
}`
    },
    {
      title: 'Take Screenshot',
      description: 'Takes a screenshot and saves to a file.',
      json: `{
  "actions": [
    { "actionType": "screenshot", "value": "my-screenshot.png" }
  ]
}`
    },
    {
      title: 'Hover Over Element',
      description: 'Hovers over an element.',
      json: `{
  "actions": [
    { "actionType": "hover", "selector": ".menu-item" }
  ]
}`
    },
    {
      title: 'Select Option',
      description: 'Selects an option in a <select> element.',
      json: `{
  "actions": [
    { "actionType": "selectOption", "selector": "#country", "value": "US" }
  ]
}`
    },
    {
      title: 'Set Value (JS)',
      description: 'Sets the value of an input using JavaScript.',
      json: `{
  "actions": [
    { "actionType": "setValue", "selector": "#coupon", "value": "DISCOUNT2025" }
  ]
}`
    }
  ];

  constructor(
    private formBuilder: FormBuilder,
    private siteConfigsService: SiteConfigsService,
    private router: Router,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {
    this.siteConfigForm = this.formBuilder.group({
      siteDomain: ['', [Validators.required, Validators.maxLength(200)]],
      productNameSelector: ['', [Validators.maxLength(500)]],
      priceSelector: ['', [Validators.maxLength(500)]],
      stockSelector: ['', [Validators.maxLength(500)]],
      sellerNameOnPageSelector: ['', [Validators.maxLength(500)]],
      imageSelector: ['', [Validators.maxLength(500)]],
      defaultUserAgent: ['', [Validators.maxLength(500)]],
      additionalHeaders: this.formBuilder.array([]),
      isEnabled: [true],
      requiresBrowserAutomation: [false],
      browserAutomationProfile: [''],
      enableSpecificationScraping: [false],
      specificationTableSelector: [''],
      specificationContainerSelector: ['']
    });

    // Note: Browser automation profile handles its own object/string conversion
  }

  ngOnInit(): void {
    this.siteConfigId = this.route.snapshot.paramMap.get('id') || undefined;
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
    this.error = null;
    this.siteConfigsService.getSiteConfig(id).subscribe({
      next: (config) => {
        this.siteConfigForm.patchValue({
          siteDomain: config.siteDomain,
          productNameSelector: config.productNameSelector,
          priceSelector: config.priceSelector,
          stockSelector: config.stockSelector,
          sellerNameOnPageSelector: config.sellerNameOnPageSelector,
          imageSelector: config.imageSelector,
          defaultUserAgent: config.defaultUserAgent,
          isEnabled: config.isEnabled,
          requiresBrowserAutomation: config.requiresBrowserAutomation ?? false,
          browserAutomationProfile: config.browserAutomationProfile ?? '',
          enableSpecificationScraping: config.enableSpecificationScraping ?? false,
          specificationTableSelector: config.specificationTableSelector ?? '',
          specificationContainerSelector: config.specificationContainerSelector ?? ''
        });

        // Load additional headers
        if (config.additionalHeaders) {
          Object.entries(config.additionalHeaders).forEach(([key, value]) => {
            this.addHeader(key, value);
          });
        }

        this.isLoading = false;
        this.error = null;
      },
      error: (error) => {
        console.error('Error loading site configuration:', error);
        this.error = 'Failed to load site configuration. Please try again.';
        this.snackBar.open('Failed to load site configuration', 'Close', { duration: 5000 });
        this.isLoading = false;
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

      const formValue = { ...this.siteConfigForm.value };
      if (typeof formValue.browserAutomationProfile === 'object') {
        formValue.browserAutomationProfile = JSON.stringify(formValue.browserAutomationProfile);
      }

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
          imageSelector: formValue.imageSelector || undefined,
          defaultUserAgent: formValue.defaultUserAgent || undefined,
          additionalHeaders: Object.keys(headersObject).length > 0 ? headersObject : undefined,
          isEnabled: formValue.isEnabled,
          requiresBrowserAutomation: formValue.requiresBrowserAutomation,
          browserAutomationProfile: formValue.browserAutomationProfile || undefined,
          enableSpecificationScraping: formValue.enableSpecificationScraping,
          specificationTableSelector: formValue.specificationTableSelector || undefined,
          specificationContainerSelector: formValue.specificationContainerSelector || undefined
        });

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
          imageSelector: formValue.imageSelector || undefined,
          defaultUserAgent: formValue.defaultUserAgent || undefined,
          additionalHeaders: Object.keys(headersObject).length > 0 ? headersObject : undefined,
          isEnabled: formValue.isEnabled,
          requiresBrowserAutomation: formValue.requiresBrowserAutomation,
          browserAutomationProfile: formValue.browserAutomationProfile || undefined,
          enableSpecificationScraping: formValue.enableSpecificationScraping,
          specificationTableSelector: formValue.specificationTableSelector || undefined,
          specificationContainerSelector: formValue.specificationContainerSelector || undefined
        });

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

  onRetry(): void {
    if (this.isEditMode && this.siteConfigId) {
      this.loadSiteConfig(this.siteConfigId);
    } else {
      this.error = null;
    }
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

  copyExampleToClipboard(text: string) {
    if (navigator && navigator.clipboard) {
      navigator.clipboard.writeText(text);
    }
  }

  toggleExamples() {
    this.showExamples = !this.showExamples;
  }

  get browserAutomationProfileControl() {
    return this.siteConfigForm.get('browserAutomationProfile') as import('@angular/forms').FormControl;
  }

  openAutomationProfileDialog() {
    const currentProfile = this.siteConfigForm.get('browserAutomationProfile')?.value;
    let profileObj: any;
    try {
      profileObj = typeof currentProfile === 'string' ? JSON.parse(currentProfile) : (currentProfile || {});
    } catch {
      profileObj = {};
    }
    const dialogRef = this.dialog.open(BrowserAutomationProfileDialogComponent, {
      data: { profile: { ...profileObj } },
      maxWidth: '98vw',
      width: '680px',
      panelClass: 'automation-profile-dialog',
      autoFocus: false
    });
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.siteConfigForm.get('browserAutomationProfile')?.setValue(result);
      }
    });
  }

  browserAutomationProfileSummary(): string {
    const val = this.siteConfigForm.get('browserAutomationProfile')?.value;
    let profile: any;
    try {
      profile = typeof val === 'string' ? JSON.parse(val) : val;
    } catch {
      return '';
    }
    if (profile && Array.isArray(profile.actions) && profile.actions.length > 0) {
      return profile.actions.map((a: any) => a.actionType).join(', ');
    }
    return 'No actions defined';
  }
}
