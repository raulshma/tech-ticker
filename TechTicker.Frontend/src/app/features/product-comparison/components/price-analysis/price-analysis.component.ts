import { Component, Input } from '@angular/core';
import {
  PriceAnalysisDto,
  ProductWithCurrentPricesDto
} from '../../services/product-comparison.service';

@Component({
  selector: 'app-price-analysis',
  templateUrl: './price-analysis.component.html',
  styleUrls: ['./price-analysis.component.scss'],
  standalone: false
})
export class PriceAnalysisComponent {
  @Input() analysis?: PriceAnalysisDto;
  @Input() product1?: ProductWithCurrentPricesDto;
  @Input() product2?: ProductWithCurrentPricesDto;

  // Expose Math object to template
  Math = Math;

  getProductName(product?: ProductWithCurrentPricesDto): string {
    if (!product) return '';
    return `${product.manufacturer || ''} ${product.name || ''}`.trim();
  }

  formatPrice(price?: number): string {
    if (price === undefined || price === null) return '$0.00';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(price);
  }

  formatPercentage(value?: number): string {
    if (value === undefined || value === null) return '0.0%';
    return `${(value >= 0 ? '+' : '')}${value.toFixed(1)}%`;
  }
}
