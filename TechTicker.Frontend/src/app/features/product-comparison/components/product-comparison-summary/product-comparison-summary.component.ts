import { Component, Input } from '@angular/core';
import {
  ProductComparisonSummaryDto,
  ProductWithCurrentPricesDto
} from '../../services/product-comparison.service';

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

  getProductImage(product?: ProductWithCurrentPricesDto): string {
    if (!product) return '/assets/images/product-placeholder.png';
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
