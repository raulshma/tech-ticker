import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTabGroup } from '@angular/material/tabs';
import {
  Chart,
  ChartConfiguration,
  ChartOptions,
  ChartType,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  LineController,
  Title,
  Tooltip,
  Legend
} from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

// Register Chart.js components
Chart.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  LineController,
  Title,
  Tooltip,
  Legend
);

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

  // Chart configuration
  public lineChartData: ChartConfiguration<'line'>['data'] = {
    labels: [],
    datasets: []
  };

  public lineChartOptions: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: {
        display: true,
        title: {
          display: true,
          text: 'Date'
        }
      },
      y: {
        display: true,
        title: {
          display: true,
          text: 'Price ($)'
        }
      }
    },
    plugins: {
      legend: {
        display: true,
        position: 'top'
      },
      tooltip: {
        mode: 'index',
        intersect: false
      }
    },
    interaction: {
      mode: 'nearest',
      axis: 'x',
      intersect: false
    }
  };

  public lineChartType = 'line' as const;

  @ViewChild(MatTabGroup) tabGroup!: MatTabGroup;
  @ViewChild(BaseChartDirective) chart: BaseChartDirective | undefined;

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
        this.updateChart();
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
    // Load price history when price history tab is selected (now at index 2)
    if (index === 2 && this.priceHistory.length === 0) {
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
    // First try normalized specifications
    if (this.product?.normalizedSpecifications && Object.keys(this.product.normalizedSpecifications).length > 0) {
      return Object.entries(this.product.normalizedSpecifications).map(([k, v]: any) => [k, (v && v.value !== undefined) ? v.value : v]);
    }

    // Then try uncategorized specifications
    if (this.product?.uncategorizedSpecifications && Object.keys(this.product.uncategorizedSpecifications).length > 0) {
      return Object.entries(this.product.uncategorizedSpecifications);
    }

    return [];
  }

  hasSpecifications(): boolean {
    const normalizedCount = this.product?.normalizedSpecifications ? Object.keys(this.product.normalizedSpecifications).length : 0;
    const uncategorizedCount = this.product?.uncategorizedSpecifications ? Object.keys(this.product.uncategorizedSpecifications).length : 0;
    return normalizedCount > 0 || uncategorizedCount > 0;
  }

  hasEnhancedSpecifications(): boolean {
    // Check if we have normalized specifications (these are considered "enhanced")
    return !!(this.product?.normalizedSpecifications && Object.keys(this.product.normalizedSpecifications).length > 0);
  }

  getEnhancedSpecifications(): any {
    if (!this.hasEnhancedSpecifications()) return null;

    const normalizedSpecs = this.product?.normalizedSpecifications;
    if (!normalizedSpecs) return null;

    // Convert normalized specifications to the format expected by ProductSpecificationsComponent
    const actualSpecs: { [key: string]: any } = {};
    const typedSpecs: { [key: string]: any } = {};
    const categorizedSpecs: { [key: string]: any } = {};

    Object.entries(normalizedSpecs).forEach(([key, value]: [string, any]) => {
      if (value && typeof value === 'object' && value.value !== undefined) {
        actualSpecs[key] = value.value;
        typedSpecs[key] = {
          value: value.value,
          type: value.dataType || 'string',
          unit: value.unit,
          confidence: value.confidence || 1.0
        };
        // Group by category if available
        const category = value.category || 'General';
        if (!categorizedSpecs[category]) {
          categorizedSpecs[category] = {};
        }
        categorizedSpecs[category][key] = value.value;
      } else {
        actualSpecs[key] = value;
        typedSpecs[key] = { value: value, type: 'string' };
        categorizedSpecs['General'] = categorizedSpecs['General'] || {};
        categorizedSpecs['General'][key] = value;
      }
    });

    return {
      isSuccess: true,
      specifications: actualSpecs,
      typedSpecifications: typedSpecs,
      categorizedSpecs: categorizedSpecs,
      metadata: {
        totalSpecifications: Object.keys(actualSpecs).length,
        processingTimeMs: 0
      },
      quality: {
        overallScore: 0.9,
        completeness: 1.0,
        accuracy: 0.9
      },
      parsingTimeMs: 0
    };
  }

  hasProductImages(): boolean {
    const productAny = this.product as any;
    return !!(productAny?.primaryImageUrl || (productAny?.additionalImageUrls && productAny.additionalImageUrls.length > 0));
  }

  getPrimaryImageUrl(): string | null {
    const productAny = this.product as any;
    return productAny?.primaryImageUrl || null;
  }

  getAdditionalImageUrls(): string[] {
    const productAny = this.product as any;
    return productAny?.additionalImageUrls || [];
  }

  updateChart(): void {
    const chartData = this.priceHistoryService.transformToChartData(this.priceHistory);
    this.lineChartData = {
      labels: chartData.labels,
      datasets: chartData.datasets
    };

    if (this.chart) {
      this.chart.update();
    }
  }
}
