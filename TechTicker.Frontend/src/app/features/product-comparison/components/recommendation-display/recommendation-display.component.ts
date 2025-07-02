import { Component, Input } from '@angular/core';
import {
  RecommendationAnalysisDto,
  ProductWithCurrentPricesDto
} from '../../services/product-comparison.service';

@Component({
  selector: 'app-recommendation-display',
  templateUrl: './recommendation-display.component.html',
  styleUrls: ['./recommendation-display.component.scss'],
  standalone: false
})
export class RecommendationDisplayComponent {
  @Input() recommendation?: RecommendationAnalysisDto;
  @Input() product1?: ProductWithCurrentPricesDto;
  @Input() product2?: ProductWithCurrentPricesDto;

  getRecommendedProduct(): ProductWithCurrentPricesDto | null {
    if (!this.recommendation || !this.product1 || !this.product2) return null;
    return this.recommendation.recommendedProductId === this.product1.productId ? this.product1 : this.product2;
  }

  getProductName(product?: ProductWithCurrentPricesDto): string {
    if (!product) return '';
    return `${product.manufacturer || ''} ${product.name || ''}`.trim();
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

  getConfidenceLevel(): string {
    if (!this.recommendation?.confidenceScore) return 'Unknown';
    if (this.recommendation.confidenceScore >= 0.8) return 'High';
    if (this.recommendation.confidenceScore >= 0.6) return 'Medium';
    return 'Low';
  }

  getConfidenceClass(): string {
    if (!this.recommendation?.confidenceScore) return 'unknown-confidence';
    if (this.recommendation.confidenceScore >= 0.8) return 'high-confidence';
    if (this.recommendation.confidenceScore >= 0.6) return 'medium-confidence';
    return 'low-confidence';
  }

  getFactorImpactClass(impact?: string): string {
    if (!impact) return 'neutral-impact';
    switch (impact.toLowerCase()) {
      case 'high':
        return 'high-impact';
      case 'medium':
        return 'medium-impact';
      case 'low':
        return 'low-impact';
      default:
        return 'neutral-impact';
    }
  }
}
