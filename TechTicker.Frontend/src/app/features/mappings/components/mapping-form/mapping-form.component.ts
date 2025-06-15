import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { 
  ProductSellerMappingDto, 
  CreateProductSellerMappingDto, 
  UpdateProductSellerMappingDto, 
  ProductDto,
  ScraperSiteConfigurationDto 
} from '../../../../shared/api/api-client';
import { MappingsService } from '../../services/mappings.service';
import { ProductsService } from '../../../products/services/products.service';

@Component({
  selector: 'app-mapping-form',
  templateUrl: './mapping-form.component.html',
  styleUrls: ['./mapping-form.component.scss'],
  standalone: false
})
export class MappingFormComponent implements OnInit {
  mappingForm: FormGroup;
  isLoading = false;
  isEditMode = false;
  mappingId: string | null = null;
  products: ProductDto[] = [];
  siteConfigurations: ScraperSiteConfigurationDto[] = [];

  constructor(
    private formBuilder: FormBuilder,
    private mappingsService: MappingsService,
    private productsService: ProductsService,
    private router: Router,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar
  ) {
    this.mappingForm = this.formBuilder.group({
      canonicalProductId: ['', [Validators.required]],
      sellerName: ['', [Validators.required, Validators.maxLength(200)]],
      exactProductUrl: ['', [Validators.required, Validators.pattern('https?://.+')]],
      isActiveForScraping: [true],
      scrapingFrequencyOverride: [''],
      siteConfigId: ['']
    });
  }

  ngOnInit(): void {
    this.mappingId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.mappingId;

    this.loadProducts();
    this.loadSiteConfigurations();

    if (this.isEditMode && this.mappingId) {
      this.loadMapping(this.mappingId);
    }
  }

  loadProducts(): void {
    this.productsService.getProducts({ pageSize: 1000 }).subscribe({
      next: (result) => {
        this.products = result.items;
      },
      error: (error) => {
        console.error('Error loading products:', error);
        this.snackBar.open('Failed to load products', 'Close', { duration: 5000 });
      }
    });
  }

  loadSiteConfigurations(): void {
    this.mappingsService.getSiteConfigurations().subscribe({
      next: (configs) => {
        this.siteConfigurations = configs;
      },
      error: (error) => {
        console.error('Error loading site configurations:', error);
        // Don't show error for this as it's optional
      }
    });
  }

  loadMapping(id: string): void {
    this.isLoading = true;
    this.mappingsService.getMapping(id).subscribe({
      next: (mapping) => {
        this.mappingForm.patchValue({
          canonicalProductId: mapping.canonicalProductId,
          sellerName: mapping.sellerName,
          exactProductUrl: mapping.exactProductUrl,
          isActiveForScraping: mapping.isActiveForScraping,
          scrapingFrequencyOverride: mapping.scrapingFrequencyOverride,
          siteConfigId: mapping.siteConfigId
        });
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading mapping:', error);
        this.snackBar.open('Failed to load mapping', 'Close', { duration: 5000 });
        this.router.navigate(['/mappings']);
      }
    });
  }

  onSubmit(): void {
    if (this.mappingForm.valid && !this.isLoading) {
      this.isLoading = true;

      const formValue = this.mappingForm.value;
      
      if (this.isEditMode && this.mappingId) {
        const updateDto = new UpdateProductSellerMappingDto({
          sellerName: formValue.sellerName,
          exactProductUrl: formValue.exactProductUrl,
          isActiveForScraping: formValue.isActiveForScraping,
          scrapingFrequencyOverride: formValue.scrapingFrequencyOverride || undefined,
          siteConfigId: formValue.siteConfigId || undefined
        });

        this.mappingsService.updateMapping(this.mappingId, updateDto).subscribe({
          next: () => {
            this.snackBar.open('Mapping updated successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/mappings']);
          },
          error: (error) => {
            console.error('Error updating mapping:', error);
            this.snackBar.open('Failed to update mapping', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      } else {
        const createDto = new CreateProductSellerMappingDto({
          canonicalProductId: formValue.canonicalProductId,
          sellerName: formValue.sellerName,
          exactProductUrl: formValue.exactProductUrl,
          isActiveForScraping: formValue.isActiveForScraping,
          scrapingFrequencyOverride: formValue.scrapingFrequencyOverride || undefined,
          siteConfigId: formValue.siteConfigId || undefined
        });

        this.mappingsService.createMapping(createDto).subscribe({
          next: () => {
            this.snackBar.open('Mapping created successfully', 'Close', { duration: 3000 });
            this.router.navigate(['/mappings']);
          },
          error: (error) => {
            console.error('Error creating mapping:', error);
            this.snackBar.open('Failed to create mapping', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      }
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel(): void {
    this.router.navigate(['/mappings']);
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
      return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} is required`;
    }
    if (control?.hasError('maxlength')) {
      const maxLength = control.errors?.['maxlength']?.requiredLength;
      return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} must be less than ${maxLength} characters`;
    }
    if (control?.hasError('pattern')) {
      return 'Please enter a valid URL (starting with http:// or https://)';
    }
    return '';
  }

  getProductDisplayName(product: ProductDto): string {
    let name = product.name || 'Unnamed Product';
    if (product.manufacturer) {
      name += ` (${product.manufacturer}`;
      if (product.modelNumber) {
        name += ` - ${product.modelNumber}`;
      }
      name += ')';
    }
    return name;
  }
}
