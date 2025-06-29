import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormControl } from '@angular/forms';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  Chart,
  ChartConfiguration,
  ChartOptions,
  ChartType,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  LineController
} from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { debounceTime, distinctUntilChanged } from 'rxjs';

// Register Chart.js components
Chart.register(
  LineController,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

import { PriceHistoryDto, ProductDto } from '../../../../shared/api/api-client';
import { PriceHistoryService, PriceHistoryFilter } from '../../services/price-history.service';
import { ProductsService } from '../../services/products.service';

@Component({
  selector: 'app-price-history',
  templateUrl: './price-history.component.html',
  styleUrls: ['./price-history.component.scss'],
  standalone: false
})
export class PriceHistoryComponent implements OnInit {
  productId!: string;
  product: ProductDto | null = null;
  priceHistory: PriceHistoryDto[] = [];
  isLoading = false;

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

  @ViewChild(BaseChartDirective) chart: BaseChartDirective | undefined;

  // Table configuration
  displayedColumns: string[] = ['timestamp', 'price', 'stockStatus', 'seller', 'actions'];
  dataSource = new MatTableDataSource<PriceHistoryDto>();

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  // Filters
  sellerControl = new FormControl('');
  startDateControl = new FormControl('');
  endDateControl = new FormControl('');
  limitControl = new FormControl(100);

  availableSellers: string[] = [];
  currentView: 'chart' | 'table' = 'chart';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private priceHistoryService: PriceHistoryService,
    private productsService: ProductsService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.productId = this.route.snapshot.paramMap.get('id')!;

    if (!this.productId) {
      this.snackBar.open('Product ID is required', 'Close', { duration: 5000 });
      this.router.navigate(['/products']);
      return;
    }

    this.setupFilters();
    this.loadProduct();
    this.loadPriceHistory();
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }

  setupFilters(): void {
    // Setup filter change listeners
    this.sellerControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(() => {
      this.loadPriceHistory();
    });

    this.startDateControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(() => {
      this.loadPriceHistory();
    });

    this.endDateControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(() => {
      this.loadPriceHistory();
    });

    this.limitControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(() => {
      this.loadPriceHistory();
    });
  }

  loadProduct(): void {
    this.productsService.getProduct(this.productId).subscribe({
      next: (product) => {
        this.product = product;
      },
      error: (error) => {
        console.error('Error loading product:', error);
        this.snackBar.open('Failed to load product details', 'Close', { duration: 5000 });
      }
    });
  }

  loadPriceHistory(): void {
    this.isLoading = true;

    const filter: PriceHistoryFilter = {
      sellerName: this.sellerControl.value || undefined,
      startDate: this.startDateControl.value ? new Date(this.startDateControl.value) : undefined,
      endDate: this.endDateControl.value ? new Date(this.endDateControl.value) : undefined,
      limit: this.limitControl.value || undefined
    };

    this.priceHistoryService.getPriceHistory(this.productId, filter).subscribe({
      next: (priceHistory) => {
        this.priceHistory = priceHistory;
        this.availableSellers = this.priceHistoryService.getUniqueSellerNames(priceHistory);
        this.updateChart();
        this.updateTable();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading price history:', error);
        this.snackBar.open('Failed to load price history', 'Close', { duration: 5000 });
        this.isLoading = false;
      }
    });
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

  updateTable(): void {
    this.dataSource.data = this.priceHistory;
  }

  clearFilters(): void {
    this.sellerControl.setValue('');
    this.startDateControl.setValue('');
    this.endDateControl.setValue('');
    this.limitControl.setValue(100);
  }

  switchView(view: 'chart' | 'table'): void {
    this.currentView = view;
  }

  goBack(): void {
    this.router.navigate(['/products']);
  }

  getSellerName(sourceUrl: string): string {
    try {
      const url = new URL(sourceUrl);
      return url.hostname.replace('www.', '');
    } catch {
      return 'Unknown';
    }
  }

  openSourceUrl(url: string): void {
    window.open(url, '_blank');
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(price);
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleString();
  }
}
