import { Component, Input } from '@angular/core';
import {
  RecommendationAnalysisDto,
  ProductWithCurrentPricesDto
} from '../../services/product-comparison.service';
import { environment } from '../../../../../environments/environment';

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
