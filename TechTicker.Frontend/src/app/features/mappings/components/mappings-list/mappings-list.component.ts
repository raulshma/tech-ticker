import { Component, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { FormControl } from '@angular/forms';
import { debounceTime, distinctUntilChanged, startWith } from 'rxjs';
import { ProductSellerMappingDto, ProductDto } from '../../../../shared/api/api-client';
import { MappingsService } from '../../services/mappings.service';
import { ProductsService } from '../../../products/services/products.service';
import { MappingDeleteDialogComponent } from '../mapping-delete-dialog/mapping-delete-dialog.component';

@Component({
  selector: 'app-mappings-list',
  templateUrl: './mappings-list.component.html',
  styleUrls: ['./mappings-list.component.scss'],
  standalone: false
})
export class MappingsListComponent implements OnInit, AfterViewInit {
  displayedColumns: string[] = ['product', 'sellerName', 'exactProductUrl', 'isActiveForScraping', 'lastScrapedAt', 'lastScrapeStatus', 'actions'];
  dataSource = new MatTableDataSource<ProductSellerMappingDto>();
  isLoading = false;

  // Filters
  productControl = new FormControl('');
  showAllMappingsControl = new FormControl(true);
  searchText = '';
  statusFilter = '';
  products: ProductDto[] = [];

  // Track scraping progress
  scrapingInProgress = new Set<string>();

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private mappingsService: MappingsService,
    private productsService: ProductsService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadProducts();
    this.setupFilters();
    this.loadMappings();
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }

  setupFilters(): void {
    // Product filter
    this.productControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.loadMappings();
      });
    // Show all mappings filter
    this.showAllMappingsControl.valueChanges
      .pipe(
        debounceTime(100),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.loadMappings();
      });
  }

  loadProducts(): void {
    this.productsService.getProducts({ pageSize: 1000 }).subscribe({
      next: (result) => {
        this.products = result.items;
      },
      error: (error) => {
        console.error('Error loading products:', error);
      }
    });
  }

  loadMappings(): void {
    this.isLoading = true;
    const productId = this.productControl.value || undefined;
    const isActiveForScraping = this.showAllMappingsControl.value ? undefined : true;

    this.mappingsService.getMappings(productId, isActiveForScraping).subscribe({
      next: (mappings) => {
        this.dataSource.data = mappings;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading mappings:', error);
        this.snackBar.open('Failed to load mappings', 'Close', { duration: 5000 });
        this.isLoading = false;
      }
    });
  }

  clearFilters(): void {
    this.productControl.setValue('');
    this.showAllMappingsControl.setValue(true);
    this.searchText = '';
    this.statusFilter = '';
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  private applyFilters(): void {
    // Set up custom filter predicate
    this.dataSource.filterPredicate = (mapping: ProductSellerMappingDto, filter: string): boolean => {
      const searchTerm = this.searchText?.toLowerCase() || '';
      
      // Search filter
      const matchesSearch = !searchTerm || 
        this.getProductName(mapping.canonicalProductId || '').toLowerCase().includes(searchTerm) ||
        (mapping.sellerName?.toLowerCase().includes(searchTerm) ?? false) ||
        (mapping.exactProductUrl?.toLowerCase().includes(searchTerm) ?? false);

      // Status filter
      const matchesStatus = !this.statusFilter ||
        (this.statusFilter === 'active' && (mapping.isActiveForScraping ?? false)) ||
        (this.statusFilter === 'inactive' && !(mapping.isActiveForScraping ?? false));

      return Boolean(matchesSearch && matchesStatus);
    };

    // Trigger filter by setting filter value
    this.dataSource.filter = 'trigger';
  }

  editMapping(mapping: ProductSellerMappingDto): void {
    if (mapping.mappingId) {
      this.router.navigate(['/mappings/edit', mapping.mappingId]);
    }
  }

  createMapping(): void {
    this.router.navigate(['/mappings/new']);
  }

  deleteMapping(mapping: ProductSellerMappingDto): void {
    if (!mapping.mappingId) {
      this.snackBar.open('Invalid mapping ID', 'Close', { duration: 3000 });
      return;
    }

    const dialogRef = this.dialog.open(MappingDeleteDialogComponent, {
      width: '400px',
      data: mapping
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result && mapping.mappingId) {
        this.mappingsService.deleteMapping(mapping.mappingId).subscribe({
          next: () => {
            this.snackBar.open('Mapping deleted successfully', 'Close', { duration: 3000 });
            this.loadMappings();
          },
          error: (error) => {
            console.error('Error deleting mapping:', error);
            this.snackBar.open('Failed to delete mapping', 'Close', { duration: 5000 });
          }
        });
      }
    });
  }

  triggerScraping(mapping: ProductSellerMappingDto): void {
    if (!mapping.mappingId) {
      this.snackBar.open('Invalid mapping ID', 'Close', { duration: 3000 });
      return;
    }

    this.scrapingInProgress.add(mapping.mappingId);
    this.mappingsService.triggerManualScraping(mapping.mappingId).subscribe({
      next: () => {
        this.snackBar.open('Scraping triggered successfully', 'Close', { duration: 3000 });
        if (mapping.mappingId) {
          this.scrapingInProgress.delete(mapping.mappingId);
        }
        // Reload mappings after a short delay to show updated status
        setTimeout(() => this.loadMappings(), 2000);
      },
      error: (error) => {
        console.error('Error triggering scraping:', error);
        this.snackBar.open('Failed to trigger scraping', 'Close', { duration: 5000 });
        if (mapping.mappingId) {
          this.scrapingInProgress.delete(mapping.mappingId);
        }
      }
    });
  }

  isScrapingInProgress(mappingId: string | undefined): boolean {
    return mappingId ? this.scrapingInProgress.has(mappingId) : false;
  }

  getProductName(productId: string): string {
    const product = this.products.find(p => p.productId === productId);
    return product?.name || 'Unknown Product';
  }

  getStatusColor(status: string | null | undefined): string {
    if (!status) return 'basic';
    
    switch (status.toLowerCase()) {
      case 'success':
      case 'completed':
        return 'primary';
      case 'pending':
      case 'in_progress':
        return 'accent';
      case 'error':
      case 'failed':
        return 'warn';
      default:
        return 'basic';
    }
  }

  getStatusIcon(status: string | null | undefined): string {
    if (!status) return 'help';
    
    switch (status.toLowerCase()) {
      case 'success':
      case 'completed':
        return 'check_circle';
      case 'pending':
      case 'in_progress':
        return 'schedule';
      case 'error':
      case 'failed':
        return 'error';
      default:
        return 'help';
    }
  }

  // Statistics methods for the dashboard
  getTotalMappings(): number {
    return this.dataSource.data.length;
  }

  getActiveMappings(): number {
    return this.dataSource.data.filter(m => m.isActiveForScraping).length;
  }

  getRecentlyScrapedMappings(): number {
    const oneDayAgo = new Date();
    oneDayAgo.setDate(oneDayAgo.getDate() - 1);
    
    return this.dataSource.data.filter(m => 
      m.lastScrapedAt && new Date(m.lastScrapedAt) > oneDayAgo
    ).length;
  }

  getFailedMappings(): number {
    return this.dataSource.data.filter(m => 
      m.lastScrapeStatus && ['error', 'failed'].includes(m.lastScrapeStatus.toLowerCase())
    ).length;
  }

  hasFilters(): boolean {
    return !!(this.searchText?.trim() || 
              this.productControl.value || 
              this.statusFilter ||
              !this.showAllMappingsControl.value);
  }
}
