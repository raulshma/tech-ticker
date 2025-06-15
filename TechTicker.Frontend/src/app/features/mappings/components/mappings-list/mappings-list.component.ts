import { Component, OnInit, ViewChild } from '@angular/core';
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
export class MappingsListComponent implements OnInit {
  displayedColumns: string[] = ['product', 'sellerName', 'exactProductUrl', 'isActiveForScraping', 'lastScrapedAt', 'lastScrapeStatus', 'actions'];
  dataSource = new MatTableDataSource<ProductSellerMappingDto>();
  isLoading = false;

  // Filters
  productControl = new FormControl('');
  products: ProductDto[] = [];

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
        startWith(''),
        debounceTime(300),
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
    
    this.mappingsService.getMappings(productId).subscribe({
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
  }

  createMapping(): void {
    this.router.navigate(['/mappings/new']);
  }

  editMapping(mapping: ProductSellerMappingDto): void {
    this.router.navigate(['/mappings/edit', mapping.mappingId]);
  }

  deleteMapping(mapping: ProductSellerMappingDto): void {
    const dialogRef = this.dialog.open(MappingDeleteDialogComponent, {
      width: '400px',
      data: mapping
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.isLoading = true;
        this.mappingsService.deleteMapping(mapping.mappingId!).subscribe({
          next: () => {
            this.snackBar.open('Mapping deleted successfully', 'Close', { duration: 3000 });
            this.loadMappings();
          },
          error: (error) => {
            console.error('Error deleting mapping:', error);
            this.snackBar.open('Failed to delete mapping', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      }
    });
  }

  getProductName(productId: string): string {
    const product = this.products.find(p => p.productId === productId);
    return product?.name || 'Unknown Product';
  }

  getStatusColor(status: string | undefined): string {
    switch (status?.toLowerCase()) {
      case 'success':
        return 'success';
      case 'error':
      case 'failed':
        return 'error';
      case 'pending':
        return 'pending';
      default:
        return 'unknown';
    }
  }
}
