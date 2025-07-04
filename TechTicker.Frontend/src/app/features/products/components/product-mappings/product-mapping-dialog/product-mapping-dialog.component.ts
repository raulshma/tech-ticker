import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { 
  ProductSellerMappingDto, 
  ScraperSiteConfigurationDto 
} from '../../../../../shared/api/api-client';

export interface ProductMappingDialogData {
  mapping: ProductSellerMappingDto | null;
  siteConfigurations: ScraperSiteConfigurationDto[];
  isEdit: boolean;
}

@Component({
  selector: 'app-product-mapping-dialog',
  templateUrl: './product-mapping-dialog.component.html',
  styleUrls: ['./product-mapping-dialog.component.scss'],
  standalone: false
})
export class ProductMappingDialogComponent implements OnInit {
  mappingForm: FormGroup;
  isLoading = false;

  constructor(
    private formBuilder: FormBuilder,
    private dialogRef: MatDialogRef<ProductMappingDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ProductMappingDialogData
  ) {
    this.mappingForm = this.formBuilder.group({
      sellerName: ['', [Validators.required, Validators.maxLength(100)]],
      exactProductUrl: ['', [Validators.required, Validators.maxLength(2048), Validators.pattern(/^https?:\/\/.+/)]],
      isActiveForScraping: [true],
      scrapingFrequencyOverride: ['', [Validators.maxLength(50)]],
      siteConfigId: ['']
    });
  }

  ngOnInit(): void {
    if (this.data.isEdit && this.data.mapping) {
      this.mappingForm.patchValue({
        sellerName: this.data.mapping.sellerName,
        exactProductUrl: this.data.mapping.exactProductUrl,
        isActiveForScraping: this.data.mapping.isActiveForScraping,
        scrapingFrequencyOverride: this.data.mapping.scrapingFrequencyOverride,
        siteConfigId: this.data.mapping.siteConfigId || ''
      });
    }
  }

  onSubmit(): void {
    if (this.mappingForm.valid) {
      const formValue = this.mappingForm.value;
      
      const result = {
        sellerName: formValue.sellerName,
        exactProductUrl: formValue.exactProductUrl,
        isActiveForScraping: formValue.isActiveForScraping,
        scrapingFrequencyOverride: formValue.scrapingFrequencyOverride || undefined,
        siteConfigId: formValue.siteConfigId || undefined
      };

      this.dialogRef.close(result);
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  getSiteConfigurationName(siteConfigId: string): string {
    const config = this.data.siteConfigurations.find(c => c.siteConfigId === siteConfigId);
    return config?.siteDomain || 'Unknown';
  }

  private markFormGroupTouched(): void {
    Object.keys(this.mappingForm.controls).forEach(key => {
      const control = this.mappingForm.get(key);
      if (control) {
        control.markAsTouched();
      }
    });
  }

  getFieldErrorMessage(fieldName: string): string {
    const control = this.mappingForm.get(fieldName);
    if (control?.hasError('required')) {
      return `${this.getFieldDisplayName(fieldName)} is required`;
    }
    if (control?.hasError('maxlength')) {
      const maxLength = control.errors?.['maxlength']?.requiredLength;
      return `${this.getFieldDisplayName(fieldName)} must be less than ${maxLength} characters`;
    }
    if (control?.hasError('pattern')) {
      return 'Please enter a valid URL starting with http:// or https://';
    }
    return '';
  }

  private getFieldDisplayName(fieldName: string): string {
    const displayNames: { [key: string]: string } = {
      'sellerName': 'Seller name',
      'exactProductUrl': 'Product URL',
      'scrapingFrequencyOverride': 'Scraping frequency'
    };
    return displayNames[fieldName] || fieldName;
  }
} 