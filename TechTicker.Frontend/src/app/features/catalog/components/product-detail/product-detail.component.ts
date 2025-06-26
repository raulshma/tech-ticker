import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTabGroup } from '@angular/material/tabs';
import { PriceHistoryDto, ProductWithCurrentPricesDto } from '../../../../shared/api/api-client';
import { CatalogService } from '../../services/catalog.service';
import { PriceHistoryService } from '../../../products/services/price-history.service';

@Component({
  selector: 'app-product-detail',
  templateUrl: './product-detail.component.html',
  styleUrls: ['./product-detail.component.scss'],
  standalone: false
})
export class ProductDetailComponent implements OnInit {
  product: ProductWithCurrentPricesDto | null = null;
  priceHistory: PriceHistoryDto[] = [];
  isLoading = true;
  isLoadingPriceHistory = false;

  @ViewChild(MatTabGroup) tabGroup!: MatTabGroup;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private catalogService: CatalogService,
    private priceHistoryService: PriceHistoryService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      const productId = params['id'];
      if (productId) {
        this.loadProduct(productId);
      }
    });
  }

  loadProduct(productId: string): void {
    this.isLoading = true;

    this.catalogService.getProductDetail(productId).subscribe({
      next: (product) => {
        this.product = product;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading product:', error);
        this.snackBar.open('Failed to load product details', 'Close', { duration: 5000 });
        this.router.navigate(['/catalog']);
      }
    });
  }

  loadPriceHistory(): void {
    if (!this.product || this.isLoadingPriceHistory) return;

    this.isLoadingPriceHistory = true;

    this.priceHistoryService.getPriceHistory(this.product.productId!, {
      limit: 100
    }).subscribe({
      next: (history) => {
        this.priceHistory = history;
        this.isLoadingPriceHistory = false;
      },
      error: (error) => {
        console.error('Error loading price history:', error);
        this.snackBar.open('Failed to load price history', 'Close', { duration: 3000 });
        this.isLoadingPriceHistory = false;
      }
    });
  }

  onTabChange(index: number): void {
    // Load price history when price history tab is selected
    if (index === 1 && this.priceHistory.length === 0) {
      this.loadPriceHistory();
    }
  }

  goBack(): void {
    this.router.navigate(['/catalog']);
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

  getProductImage(): string {
    return 'assets/images/product-placeholder.png';
  }

  getProductTitle(): string {
    if (!this.product) return '';

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
    return this.product?.category?.name || 'Uncategorized';
  }

  getBestPrice(): number | null {
    return this.product?.lowestCurrentPrice || null;
  }

  getWorstPrice(): number | null {
    return this.product?.highestCurrentPrice || null;
  }

  getSavingsAmount(): number | null {
    const best = this.getBestPrice();
    const worst = this.getWorstPrice();
    if (best && worst && worst > best) {
      return worst - best;
    }
    return null;
  }

  getSavingsPercentage(): number | null {
    const best = this.getBestPrice();
    const worst = this.getWorstPrice();
    if (best && worst && worst > best) {
      return Math.round(((worst - best) / worst) * 100);
    }
    return null;
  }

  openSellerLink(sourceUrl: string): void {
    window.open(sourceUrl, '_blank');
  }

  getSpecificationEntries(): [string, any][] {
    if (!this.product?.specifications) return [];
    return Object.entries(this.product.specifications);
  }

  hasSpecifications(): boolean {
    return this.getSpecificationEntries().length > 0;
  }
}
