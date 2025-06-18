import { Component, OnInit, ViewChild } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { ScraperSiteConfigurationDto } from '../../../../shared/api/api-client';
import { SiteConfigsService } from '../../services/site-configs.service';
import { SiteConfigDeleteDialogComponent } from '../site-config-delete-dialog/site-config-delete-dialog.component';

@Component({
  selector: 'app-site-configs-list',
  templateUrl: './site-configs-list.component.html',
  styleUrls: ['./site-configs-list.component.scss'],
  standalone: false
})
export class SiteConfigsListComponent implements OnInit {
  displayedColumns: string[] = ['siteDomain', 'selectors', 'isEnabled', 'createdAt', 'actions'];
  dataSource = new MatTableDataSource<ScraperSiteConfigurationDto>();
  isLoading = false;

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private siteConfigsService: SiteConfigsService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadSiteConfigs();
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }

  loadSiteConfigs(): void {
    this.isLoading = true;
    this.siteConfigsService.getSiteConfigs().subscribe({
      next: (configs) => {
        this.dataSource.data = configs;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading site configurations:', error);
        this.snackBar.open('Failed to load site configurations', 'Close', { duration: 5000 });
        this.isLoading = false;
      }
    });
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();

    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  createSiteConfig(): void {
    this.router.navigate(['/site-configs/new']);
  }

  openAiGenerator(): void {
    this.router.navigate(['/site-configs/ai-generator']);
  }

  editSiteConfig(config: ScraperSiteConfigurationDto): void {
    this.router.navigate(['/site-configs/edit', config.siteConfigId]);
  }

  deleteSiteConfig(config: ScraperSiteConfigurationDto): void {
    const dialogRef = this.dialog.open(SiteConfigDeleteDialogComponent, {
      width: '400px',
      data: config
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.isLoading = true;
        this.siteConfigsService.deleteSiteConfig(config.siteConfigId!).subscribe({
          next: () => {
            this.snackBar.open('Site configuration deleted successfully', 'Close', { duration: 3000 });
            // The service will automatically update the local list
          },
          error: (error) => {
            console.error('Error deleting site configuration:', error);
            this.snackBar.open('Failed to delete site configuration', 'Close', { duration: 5000 });
            this.isLoading = false;
          }
        });
      }
    });
  }

  getSelectorsCount(config: ScraperSiteConfigurationDto): number {
    let count = 0;
    if (config.productNameSelector) count++;
    if (config.priceSelector) count++;
    if (config.stockSelector) count++;
    if (config.sellerNameOnPageSelector) count++;
    return count;
  }

  getSelectorsText(config: ScraperSiteConfigurationDto): string {
    const selectors = [];
    if (config.productNameSelector) selectors.push('Name');
    if (config.priceSelector) selectors.push('Price');
    if (config.stockSelector) selectors.push('Stock');
    if (config.sellerNameOnPageSelector) selectors.push('Seller');

    return selectors.length > 0 ? selectors.join(', ') : 'None configured';
  }
}
