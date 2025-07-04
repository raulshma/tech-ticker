import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { FormControl } from '@angular/forms';
import { ProductDto } from '../../../../shared/api/api-client';
import { environment } from '../../../../../environments/environment';
import { MatAutocompleteTrigger } from '@angular/material/autocomplete';
import { ChangeDetectorRef } from '@angular/core';

@Component({
  selector: 'app-product-selector',
  templateUrl: './product-selector.component.html',
  styleUrls: ['./product-selector.component.scss'],
  standalone: false
})
export class ProductSelectorComponent implements OnInit, OnDestroy {
  @Input() label: string = 'Select Product';
  @Input() placeholder: string = 'Search for product...';
  @Input() searchControl!: FormControl;
  @Input() set options(value: ProductDto[]) {
    this._options = value;

    // Automatically open the autocomplete panel when options are loaded
    if (this.autocompleteTrigger && value && value.length > 0) {
      // Use a timeout to ensure the panel opens after Angular updates the view
      setTimeout(() => {
        this.autocompleteTrigger.openPanel();
        this.cdr.markForCheck();
      });
    }
  }

  get options(): ProductDto[] {
    return this._options;
  }

  private _options: ProductDto[] = [];
  @Input() selectedProduct: ProductDto | null = null;
  @Input() required: boolean = false;
  @Input() disabled: boolean = false;

  @Output() productSelected = new EventEmitter<ProductDto>();

  @ViewChild(MatAutocompleteTrigger) private autocompleteTrigger!: MatAutocompleteTrigger;

  constructor(private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    // Initialize search control if not provided
    if (!this.searchControl) {
      this.searchControl = new FormControl('');
    }
  }

  ngOnDestroy(): void {
    // Component cleanup if needed
  }

  onProductSelected(product: ProductDto): void {
    // Emit the event first so parent components can react.
    this.productSelected.emit(product);

    // Update the input so that it displays the chosen product name rather than
    // the plain object. This also effectively closes the autocomplete panel.
    this.searchControl.setValue(this.displayProductName(product));
  }

  trackByProduct(index: number, product: ProductDto): string {
    return product.productId || index.toString();
  }

  // Use the same formatter for both ProductDto objects coming from the autocomplete
  // and raw string values typed by the user so that the control can display the
  // search text while the user is typing. This prevents the input text from
  // disappearing when the value is still a string (before an option is
  // selected).
  displayProductName = (value: ProductDto | string | null): string => {
    if (!value) {
      return '';
    }

    // When the value is a plain string (user is typing) just return it as-is so
    // it stays visible in the input field.
    if (typeof value === 'string') {
      return value;
    }

    // Otherwise format the ProductDto nicely for display.
    return `${value.manufacturer || ''} ${value.name || ''}`.trim();
  };

  hasProductImages(product: ProductDto): boolean {
    return !!(product.primaryImageUrl || (product.additionalImageUrls && product.additionalImageUrls.length > 0));
  }

  getPrimaryImageUrl(product: ProductDto): string | null {
    return product.primaryImageUrl || null;
  }

  getAdditionalImageUrls(product: ProductDto): string[] {
    return product.additionalImageUrls || [];
  }

  getProductImage(product: ProductDto): string {
    if (product.primaryImageUrl) {
      return this.getImageUrl(product.primaryImageUrl);
    }
    if (product.additionalImageUrls && product.additionalImageUrls.length > 0) {
      return this.getImageUrl(product.additionalImageUrls[0]);
    }
    return '/assets/images/product-placeholder.png';
  }

  private getImageUrl(imageUrl: string): string {
    // Convert relative paths to absolute URLs
    if (imageUrl && !imageUrl.startsWith('http')) {
      // Use the API base URL from environment
      const baseUrl = this.getApiBaseUrl();
      return `${baseUrl}/${imageUrl}`;
    }
    return imageUrl;
  }

  private getApiBaseUrl(): string {
    // Get the API base URL from environment, fallback to current location
    return environment.apiUrl || window.location.origin;
  }

  onImageError(event: Event): void {
    const target = event.target as HTMLImageElement;
    target.src = '/assets/images/product-placeholder.png';
  }

  getProductCategory(product: ProductDto): string {
    return product.category?.name || 'Unknown Category';
  }

  clearSelection(): void {
    this.searchControl.setValue('');
    this.productSelected.emit(null as any); // Emit null to clear selection properly
  }

  getKeySpecs(product: ProductDto): { key: string, value: string }[] {
    if (!product) return [];

    const specs: { key: string, value: string }[] = [];

    // First try normalized specifications
    if (product.normalizedSpecifications && Object.keys(product.normalizedSpecifications).length > 0) {
      const importantKeys = ['gpu_model', 'memory_type', 'gpu_clock', 'stream_processors', 'memory_clock', 'recommended_psu', 'brand', 'weight'];

      for (const key of importantKeys) {
        if (product.normalizedSpecifications[key] && specs.length < 4) {
          const spec = product.normalizedSpecifications[key];
          const value = (spec && typeof spec === 'object' && spec.value !== undefined) ? spec.value : spec;
          const displayKey = this.formatSpecificationKey(key);
          specs.push({
            key: displayKey,
            value: String(value)
          });
        }
      }

      // If we don't have enough important specs, add any other normalized specs
      if (specs.length < 4) {
        for (const [key, value] of Object.entries(product.normalizedSpecifications)) {
          if (!importantKeys.includes(key) && specs.length < 4) {
            const displayValue = (value && typeof value === 'object' && value.value !== undefined) ? value.value : value;
            const displayKey = this.formatSpecificationKey(key);
            specs.push({
              key: displayKey,
              value: String(displayValue)
            });
          }
        }
      }
    }

    // If still not enough specs, try uncategorized specifications
    if (specs.length < 4 && product.uncategorizedSpecifications && Object.keys(product.uncategorizedSpecifications).length > 0) {
      const remainingCount = 4 - specs.length;
      const uncategorizedEntries = Object.entries(product.uncategorizedSpecifications).slice(0, remainingCount);

      for (const [key, value] of uncategorizedEntries) {
        specs.push({
          key: key,
          value: String(value)
        });
      }
    }

    return specs;
  }

  private formatSpecificationKey(key: string): string {
    // Convert snake_case to Title Case
    return key
      .split('_')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
      .join(' ');
  }

  hasSpecifications(product: ProductDto | null): boolean {
    if (!product) return false;

    const normalizedCount = product.normalizedSpecifications ? Object.keys(product.normalizedSpecifications).length : 0;
    const uncategorizedCount = product.uncategorizedSpecifications ? Object.keys(product.uncategorizedSpecifications).length : 0;

    return normalizedCount > 0 || uncategorizedCount > 0;
  }
}
