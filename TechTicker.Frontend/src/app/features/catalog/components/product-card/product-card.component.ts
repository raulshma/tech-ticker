import { Component, Input, Output, EventEmitter } from '@angular/core';
import { ProductWithCurrentPricesDto, CurrentPriceDto } from '../../../../shared/api/api-client';
import { CatalogService } from '../../services/catalog.service';

@Component({
  selector: 'app-product-card',
  templateUrl: './product-card.component.html',
  styleUrls: ['./product-card.component.scss'],
  standalone: false
})
export class ProductCardComponent {
  @Input() product!: ProductWithCurrentPricesDto;
  @Input() viewMode: 'grid' | 'list' = 'grid';
  @Output() productClick = new EventEmitter<ProductWithCurrentPricesDto>();

  constructor(private catalogService: CatalogService) {}

  onCardClick(): void {
    this.productClick.emit(this.product);
  }

  formatPrice(price: number): string {
    return this.catalogService.formatPrice(price);
  }

  getStockStatusColor(stockStatus: string): string {
    return this.catalogService.getStockStatusColor(stockStatus);
  }

  getStockStatusIcon(stockStatus: string): string {
    return this.catalogService.getStockStatusIcon(stockStatus);
  }

  extractSellerName(sourceUrl: string): string {
    return this.catalogService.extractSellerName(sourceUrl);
  }

  hasProductImages(): boolean {
    const productAny = this.product as any;
    return !!(productAny.primaryImageUrl || (productAny.additionalImageUrls && productAny.additionalImageUrls.length > 0));
  }

  getPrimaryImageUrl(): string | null {
    const productAny = this.product as any;
    return productAny.primaryImageUrl || null;
  }

  getAdditionalImageUrls(): string[] {
    const productAny = this.product as any;
    return productAny.additionalImageUrls || [];
  }

  getBestPrice(): number | null {
    return this.product.lowestCurrentPrice || null;
  }

  getPriceRange(): string {
    if (!this.product.currentPrices || this.product.currentPrices.length === 0) {
      return 'No prices available';
    }

    if (this.product.currentPrices.length === 1) {
      return this.formatPrice(this.product.currentPrices[0].price!);
    }

    const lowest = this.product.lowestCurrentPrice!;
    const highest = this.product.highestCurrentPrice!;

    if (lowest === highest) {
      return this.formatPrice(lowest);
    }

    return `${this.formatPrice(lowest)} - ${this.formatPrice(highest)}`;
  }

  getAvailableSellers(): string[] {
    if (!this.product.currentPrices) return [];

    return this.product.currentPrices
      .map((price: CurrentPriceDto) => this.extractSellerName(price.sourceUrl!))
      .slice(0, 3); // Show max 3 sellers in card
  }

  hasMultipleSellers(): boolean {
    return this.product.availableSellersCount! > 1;
  }

  getProductImage(): string {
    // Placeholder image - in a real app, this would come from the product data
    return 'assets/images/product-placeholder.png';
  }

  getProductTitle(): string {
    let title = this.product.name || 'Unknown Product';
    if (this.product.manufacturer) {
      title = `${this.product.manufacturer} ${title}`;
    }
    if (this.product.modelNumber) {
      title += ` (${this.product.modelNumber})`;
    }
    return title;
  }

  getCategoryName(): string {
    return this.product.category?.name || 'Uncategorized';
  }

  isInStock(): boolean {
    if (!this.product.currentPrices || this.product.currentPrices.length === 0) {
      return false;
    }

    return this.product.currentPrices.some((price: CurrentPriceDto) =>
      price.stockStatus?.toLowerCase().includes('stock') ||
      price.stockStatus?.toLowerCase().includes('available')
    );
  }

  getStockStatus(): string {
    if (!this.product.currentPrices || this.product.currentPrices.length === 0) {
      return 'No stock information';
    }

    const inStockCount = this.product.currentPrices.filter((price: CurrentPriceDto) =>
      price.stockStatus?.toLowerCase().includes('stock') ||
      price.stockStatus?.toLowerCase().includes('available')
    ).length;

    if (inStockCount === 0) {
      return 'Out of stock';
    } else if (inStockCount === this.product.currentPrices.length) {
      return 'In stock';
    } else {
      return `${inStockCount}/${this.product.currentPrices.length} sellers have stock`;
    }
  }

  getKeySpecs(): string[] {
    // First try normalized specifications
    if (this.product.normalizedSpecifications && Object.keys(this.product.normalizedSpecifications).length > 0) {
      return Object.entries(this.product.normalizedSpecifications)
        .slice(0, 3)
        .map(([key, value]: [string, any]) => {
          const displayValue = (value && typeof value === 'object' && value.value !== undefined) ? value.value : value;
          return `${key}: ${displayValue}`;
        });
    }

    // Then try uncategorized specifications
    if (this.product.uncategorizedSpecifications && Object.keys(this.product.uncategorizedSpecifications).length > 0) {
      return Object.entries(this.product.uncategorizedSpecifications)
        .slice(0, 3)
        .map(([key, value]) => `${key}: ${value}`);
    }

    return [];
  }
}
