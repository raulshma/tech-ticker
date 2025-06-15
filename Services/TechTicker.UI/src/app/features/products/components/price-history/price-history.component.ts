import { Component, OnInit, Input } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { finalize } from 'rxjs/operators';
import {
  PriceHistoryService,
  PriceHistoryEntry,
  PriceHistoryStats,
  ChartDataPoint,
  PriceHistorySearchParams
} from '../../services/price-history.service';

@Component({
  selector: 'app-price-history',
  templateUrl: './price-history.component.html',
  styleUrls: ['./price-history.component.css'],
  standalone: false
})
export class PriceHistoryComponent implements OnInit {
  @Input() canonicalProductId!: string;
  @Input() productName?: string;

  priceHistory: PriceHistoryEntry[] = [];
  priceStats: PriceHistoryStats | null = null;
  chartData: ChartDataPoint[] = [];
  availableSellers: string[] = [];
  priceComparison: any[] = [];

  loading = false;
  chartLoading = false;
  statsLoading = false;

  // Filters
  selectedSeller = '';
  selectedTimeRange = '30d';
  startDate = '';
  endDate = '';

  // Table pagination
  pageIndex = 1;
  pageSize = 20;
  total = 0;

  // Chart options
  chartOptions: any = {};
  showChart = true;

  // Time range options
  timeRangeOptions = [
    { label: '7 Days', value: '7d' },
    { label: '30 Days', value: '30d' },
    { label: '90 Days', value: '90d' },
    { label: '1 Year', value: '365d' },
    { label: 'Custom', value: 'custom' }
  ];

  constructor(
    private priceHistoryService: PriceHistoryService,
    private message: NzMessageService
  ) {}

  ngOnInit(): void {
    if (!this.canonicalProductId) {
      this.message.error('Product ID is required to display price history');
      return;
    }

    this.loadInitialData();
  }

  loadInitialData(): void {
    this.loadPriceStats();
    this.loadAvailableSellers();
    this.loadPriceHistory();
    this.loadChartData();
    this.loadPriceComparison();
  }

  loadPriceHistory(): void {
    this.loading = true;

    const params: PriceHistorySearchParams = {
      canonicalProductId: this.canonicalProductId,
      page: this.pageIndex,
      pageSize: this.pageSize,
      sortBy: 'scrapedAt',
      sortOrder: 'desc'
    };

    if (this.selectedSeller) {
      params.sellerName = this.selectedSeller;
    }

    if (this.selectedTimeRange === 'custom' && this.startDate && this.endDate) {
      params.startDate = this.startDate;
      params.endDate = this.endDate;
    }

    this.priceHistoryService.getPriceHistory(params)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (response) => {
          this.priceHistory = response.items;
          this.total = response.total;
        },
        error: (error) => {
          console.error('Error loading price history:', error);
          this.message.error('Failed to load price history');
        }
      });
  }

  loadPriceStats(): void {
    this.statsLoading = true;

    const timeRange = this.selectedTimeRange !== 'custom' ? this.selectedTimeRange : undefined;

    this.priceHistoryService.getPriceHistoryStats(this.canonicalProductId, timeRange)
      .pipe(finalize(() => this.statsLoading = false))
      .subscribe({
        next: (stats) => {
          this.priceStats = stats;
        },
        error: (error) => {
          console.error('Error loading price stats:', error);
          this.message.error('Failed to load price statistics');
        }
      });
  }

  loadChartData(): void {
    if (!this.showChart) return;

    this.chartLoading = true;

    const timeRange = this.selectedTimeRange !== 'custom' ? this.selectedTimeRange : undefined;

    this.priceHistoryService.getPriceHistoryForChart(
      this.canonicalProductId,
      timeRange,
      this.selectedSeller || undefined
    )
    .pipe(finalize(() => this.chartLoading = false))
    .subscribe({
      next: (data) => {
        this.chartData = data;
        this.updateChartOptions();
      },
      error: (error) => {
        console.error('Error loading chart data:', error);
        this.message.error('Failed to load chart data');
      }
    });
  }

  loadAvailableSellers(): void {
    this.priceHistoryService.getAvailableSellers(this.canonicalProductId)
      .subscribe({
        next: (sellers) => {
          this.availableSellers = sellers;
        },
        error: (error) => {
          console.error('Error loading sellers:', error);
        }
      });
  }

  loadPriceComparison(): void {
    this.priceHistoryService.getPriceComparison(this.canonicalProductId)
      .subscribe({
        next: (comparison) => {
          this.priceComparison = comparison;
        },
        error: (error) => {
          console.error('Error loading price comparison:', error);
        }
      });
  }

  updateChartOptions(): void {
    if (!this.chartData || this.chartData.length === 0) {
      this.chartOptions = {};
      return;
    }

    // Group data by seller for multiple series
    const seriesData: { [seller: string]: any[] } = {};

    this.chartData.forEach(point => {
      if (!seriesData[point.seller]) {
        seriesData[point.seller] = [];
      }
      seriesData[point.seller].push([
        new Date(point.date).getTime(),
        point.price
      ]);
    });

    const series = Object.keys(seriesData).map(seller => ({
      name: seller,
      data: seriesData[seller].sort((a, b) => a[0] - b[0]),
      type: 'line',
      smooth: true
    }));

    this.chartOptions = {
      title: {
        text: `Price History - ${this.productName || 'Product'}`,
        left: 'center'
      },
      tooltip: {
        trigger: 'axis',
        formatter: (params: any) => {
          let result = `<strong>${new Date(params[0].data[0]).toLocaleDateString()}</strong><br/>`;
          params.forEach((param: any) => {
            result += `${param.seriesName}: $${param.data[1].toFixed(2)}<br/>`;
          });
          return result;
        }
      },
      legend: {
        data: Object.keys(seriesData),
        top: 30
      },
      xAxis: {
        type: 'time',
        name: 'Date'
      },
      yAxis: {
        type: 'value',
        name: 'Price ($)',
        axisLabel: {
          formatter: '${value}'
        }
      },
      series: series,
      dataZoom: [
        {
          type: 'inside',
          start: 0,
          end: 100
        },
        {
          start: 0,
          end: 100,
          handleIcon: 'M10.7,11.9v-1.3H9.3v1.3c-4.9,0.3-8.8,4.4-8.8,9.4c0,5,3.9,9.1,8.8,9.4v1.3h1.3v-1.3c4.9-0.3,8.8-4.4,8.8-9.4C19.5,16.3,15.6,12.2,10.7,11.9z M13.3,24.4H6.7V23.1h6.6V24.4z M13.3,19.6H6.7v-1.4h6.6V19.6z',
          handleSize: '80%',
          handleStyle: {
            color: '#fff',
            shadowBlur: 3,
            shadowColor: 'rgba(0, 0, 0, 0.6)',
            shadowOffsetX: 2,
            shadowOffsetY: 2
          }
        }
      ]
    };
  }

  onTimeRangeChange(): void {
    if (this.selectedTimeRange !== 'custom') {
      this.startDate = '';
      this.endDate = '';
    }
    this.pageIndex = 1;
    this.loadPriceHistory();
    this.loadPriceStats();
    this.loadChartData();
  }

  onSellerChange(): void {
    this.pageIndex = 1;
    this.loadPriceHistory();
    this.loadChartData();
  }

  onCustomDateChange(): void {
    if (this.selectedTimeRange === 'custom' && this.startDate && this.endDate) {
      this.pageIndex = 1;
      this.loadPriceHistory();
      this.loadPriceStats();
      this.loadChartData();
    }
  }

  onPageChange(pageIndex: number): void {
    this.pageIndex = pageIndex;
    this.loadPriceHistory();
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.pageIndex = 1;
    this.loadPriceHistory();
  }

  toggleChart(): void {
    this.showChart = !this.showChart;
    if (this.showChart && this.chartData.length === 0) {
      this.loadChartData();
    }
  }

  exportData(): void {
    const params: PriceHistorySearchParams = {
      canonicalProductId: this.canonicalProductId,
      sellerName: this.selectedSeller || undefined,
      startDate: this.startDate || undefined,
      endDate: this.endDate || undefined
    };

    this.priceHistoryService.exportPriceHistory(this.canonicalProductId, 'csv', params)
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `price-history-${this.canonicalProductId}.csv`;
          link.click();
          window.URL.revokeObjectURL(url);
          this.message.success('Price history exported successfully');
        },
        error: (error) => {
          console.error('Error exporting data:', error);
          this.message.error('Failed to export price history');
        }
      });
  }

  getPriceChangeClass(change: number): string {
    if (change > 0) return 'price-increase';
    if (change < 0) return 'price-decrease';
    return 'price-stable';
  }

  getPriceChangeIcon(change: number): string {
    if (change > 0) return 'arrow-up';
    if (change < 0) return 'arrow-down';
    return 'minus';
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }
}
