import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { FormControl } from '@angular/forms';
import { ProductDto } from '../../../../shared/api/api-client';

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
  @Input() options: ProductDto[] = [];
  @Input() selectedProduct: ProductDto | null = null;
  @Input() required: boolean = false;
  @Input() disabled: boolean = false;

  @Output() productSelected = new EventEmitter<ProductDto>();

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
    this.productSelected.emit(product);
  }

  displayProductName(product: ProductDto | null): string {
    if (!product) return '';
    return `${product.manufacturer || ''} ${product.name || ''}`.trim();
  }

  getProductImage(product: ProductDto): string {
    if (product.primaryImageUrl) {
      return product.primaryImageUrl;
    }
    if (product.additionalImageUrls && product.additionalImageUrls.length > 0) {
      return product.additionalImageUrls[0];
    }
    return '/assets/images/product-placeholder.png';
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
    this.productSelected.emit({} as ProductDto); // Emit empty product to clear selection
  }

  getKeySpecs(specifications: { [key: string]: any } | undefined): { key: string, value: string }[] {
    if (!specifications) return [];

    // Get the first 3-4 most important specifications for display
    const importantKeys = ['CPU', 'GPU', 'RAM', 'Storage', 'Display', 'Screen Size', 'Resolution', 'Price'];
    const specs: { key: string, value: string }[] = [];

    for (const key of importantKeys) {
      if (specifications[key] && specs.length < 4) {
        specs.push({
          key: key,
          value: String(specifications[key])
        });
      }
    }

    // If we don't have enough important specs, add any other specs
    if (specs.length < 4) {
      for (const [key, value] of Object.entries(specifications)) {
        if (!importantKeys.includes(key) && specs.length < 4) {
          specs.push({
            key: key,
            value: String(value)
          });
        }
      }
    }

    return specs;
  }
}
