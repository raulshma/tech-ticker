import { Component, Input } from '@angular/core';
import {
  ProductComparisonSummaryDto,
  ProductWithCurrentPricesDto
} from '../../services/product-comparison.service';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-product-comparison-summary',
  templateUrl: './product-comparison-summary.component.html',
  styleUrls: ['./product-comparison-summary.component.scss'],
  standalone: false
})
export class ProductComparisonSummaryComponent {
  @Input() summary?: ProductComparisonSummaryDto;
  @Input() product1?: ProductWithCurrentPricesDto;
  @Input() product2?: ProductWithCurrentPricesDto;

  getWinnerProduct(): ProductWithCurrentPricesDto | null {
    if (!this.summary || !this.product1 || !this.product2) return null;
    return this.summary.recommendedProductId === this.product1.productId ? this.product1 : this.product2;
  }

  getLoserProduct(): ProductWithCurrentPricesDto | null {
    if (!this.summary || !this.product1 || !this.product2) return null;
    return this.summary.recommendedProductId === this.product1.productId ? this.product2 : this.product1;
  }

  hasProductImages(product?: ProductWithCurrentPricesDto): boolean {
    if (!product) return false;
    return !!(product.primaryImageUrl || (product.additionalImageUrls && product.additionalImageUrls.length > 0));
  }

  getPrimaryImageUrl(product?: ProductWithCurrentPricesDto): string | null {
    if (!product) return null;
    return product.primaryImageUrl || null;
  }

  getAdditionalImageUrls(product?: ProductWithCurrentPricesDto): string[] {
    if (!product) return [];
    return product.additionalImageUrls || [];
  }

  getProductImage(product?: ProductWithCurrentPricesDto): string {
    if (!product) return '/assets/images/product-placeholder.png';
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

  getProductName(product?: ProductWithCurrentPricesDto): string {
    if (!product) return '';
    return `${product.manufacturer || ''} ${product.name || ''}`.trim();
  }

  getScoreDifference(): number {
    if (!this.summary?.product1OverallScore || !this.summary?.product2OverallScore) return 0;
    return Math.abs(this.summary.product1OverallScore - this.summary.product2OverallScore);
  }

  getScorePercentage(score?: number): number {
    return Math.round((score || 0) * 100);
  }

  getMatchingPercentage(): number {
    if (!this.summary?.totalSpecifications || this.summary.totalSpecifications === 0) return 0;
    return Math.round(((this.summary.matchingSpecifications || 0) / this.summary.totalSpecifications) * 100);
  }
}
