import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzMessageModule, NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzStatisticModule } from 'ng-zorro-antd/statistic';
import { finalize } from 'rxjs/operators';
import { ProductSellerMapping, ProductSellerMappingService, ProductSellerMappingSearchParams } from '../../services/product-seller-mapping.service';

@Component({
  selector: 'app-mapping-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NzTableModule,
    NzButtonModule,
    NzInputModule,
    NzSelectModule,
    NzFormModule,
    NzCardModule,
    NzTagModule,
    NzMessageModule,
    NzModalModule,
    NzIconModule,
    NzGridModule,
    NzEmptyModule,
    NzToolTipModule,
    NzBadgeModule,
    NzStatisticModule
  ],
  templateUrl: './mapping-list.component.html',
  styleUrls: ['./mapping-list.component.css']
})
export class MappingListComponent implements OnInit {
  mappings: ProductSellerMapping[] = [];
  loading = false;
  total = 0;
  pageIndex = 1;
  pageSize = 10;
  sortField: string | null = null;
  sortOrder: string | null = null;
  searchValue = '';
  sellerFilter = '';
  canonicalProductFilter = '';
  siteConfigFilter = '';
  activeFilter: boolean | null = null;

  constructor(
    private mappingService: ProductSellerMappingService,
    private router: Router,
    private message: NzMessageService,
    private modal: NzModalService
  ) {}

  ngOnInit(): void {
    this.loadMappings();
  }

  loadMappings(): void {
    this.loading = true;

    const params: ProductSellerMappingSearchParams = {
      page: this.pageIndex,
      pageSize: this.pageSize,
      sortBy: this.sortField || undefined,
      sortOrder: this.sortOrder === 'ascend' ? 'asc' : this.sortOrder === 'descend' ? 'desc' : undefined
    };

    if (this.sellerFilter) {
      params.sellerName = this.sellerFilter;
    }

    if (this.canonicalProductFilter) {
      params.canonicalProductId = this.canonicalProductFilter;
    }

    if (this.siteConfigFilter) {
      params.siteConfigId = this.siteConfigFilter;
    }

    if (this.activeFilter !== null) {
      params.isActiveForScraping = this.activeFilter;
    }

    this.mappingService.getMappings(params)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (response) => {
          this.mappings = response.items;
          this.total = response.total;
        },
        error: (error) => {
          console.error('Error loading mappings:', error);
          this.message.error('Failed to load product-seller mappings');
        }
      });
  }

  onQueryParamsChange(params: any): void {
    this.pageIndex = params.pageIndex;
    this.pageSize = params.pageSize;
    this.sortField = params.sort?.[0]?.key || null;
    this.sortOrder = params.sort?.[0]?.value || null;
    this.loadMappings();
  }

  onSearch(): void {
    this.pageIndex = 1;
    this.loadMappings();
  }

  onResetFilters(): void {
    this.searchValue = '';
    this.sellerFilter = '';
    this.canonicalProductFilter = '';
    this.siteConfigFilter = '';
    this.activeFilter = null;
    this.pageIndex = 1;
    this.loadMappings();
  }

  createMapping(): void {
    this.router.navigate(['/mappings/product-seller/create']);
  }

  viewMapping(id: string): void {
    this.router.navigate(['/mappings/product-seller', id]);
  }

  editMapping(id: string): void {
    this.router.navigate(['/mappings/product-seller', id, 'edit']);
  }

  deleteMapping(mapping: ProductSellerMapping): void {
    this.modal.confirm({
      nzTitle: 'Delete Product-Seller Mapping',
      nzContent: `Are you sure you want to delete the mapping for "${mapping.canonicalProduct?.name}" on "${mapping.sellerName}"? This action cannot be undone.`,
      nzOkText: 'Delete',
      nzOkType: 'primary',
      nzOkDanger: true,
      nzCancelText: 'Cancel',
      nzOnOk: () => {
        this.loading = true;
        this.mappingService.deleteMapping(mapping.id)
          .pipe(finalize(() => this.loading = false))
          .subscribe({
            next: () => {
              this.message.success('Product-seller mapping deleted successfully');
              this.loadMappings();
            },
            error: (error) => {
              console.error('Error deleting mapping:', error);
              this.message.error('Failed to delete product-seller mapping');
            }
          });
      }
    });
  }

  toggleScrapingStatus(mapping: ProductSellerMapping): void {
    const newStatus = !mapping.isActiveForScraping;
    const action = newStatus ? 'enable' : 'disable';

    this.modal.confirm({
      nzTitle: `${action.charAt(0).toUpperCase() + action.slice(1)} Scraping`,
      nzContent: `Are you sure you want to ${action} scraping for this mapping?`,
      nzOkText: action.charAt(0).toUpperCase() + action.slice(1),
      nzOkType: 'primary',
      nzCancelText: 'Cancel',
      nzOnOk: () => {
        this.mappingService.toggleScrapingStatus(mapping.id, newStatus)
          .subscribe({
            next: () => {
              this.message.success(`Scraping ${action}d successfully`);
              this.loadMappings();
            },
            error: (error) => {
              console.error(`Error ${action}ing scraping:`, error);
              this.message.error(`Failed to ${action} scraping`);
            }
          });
      }
    });
  }

  triggerScraping(mapping: ProductSellerMapping): void {
    this.modal.confirm({
      nzTitle: 'Trigger Immediate Scraping',
      nzContent: `Do you want to trigger immediate scraping for "${mapping.canonicalProduct?.name}" on "${mapping.sellerName}"?`,
      nzOkText: 'Trigger Scraping',
      nzOkType: 'primary',
      nzCancelText: 'Cancel',
      nzOnOk: () => {
        this.mappingService.triggerScraping(mapping.id)
          .subscribe({
            next: (result) => {
              if (result.success) {
                this.message.success('Scraping triggered successfully');
              } else {
                this.message.warning(result.message);
              }
            },
            error: (error) => {
              console.error('Error triggering scraping:', error);
              this.message.error('Failed to trigger scraping');
            }
          });
      }
    });
  }

  getScrapingStatusTag(mapping: ProductSellerMapping): { color: string; text: string } {
    if (!mapping.isActiveForScraping) {
      return { color: 'default', text: 'Disabled' };
    }

    if (mapping.lastScrapedAt) {
      const lastScrapeDate = new Date(mapping.lastScrapedAt);
      const now = new Date();
      const hoursSinceLastScrape = (now.getTime() - lastScrapeDate.getTime()) / (1000 * 60 * 60);

      if (hoursSinceLastScrape < 24) {
        return { color: 'green', text: 'Active' };
      } else if (hoursSinceLastScrape < 72) {
        return { color: 'orange', text: 'Stale' };
      } else {
        return { color: 'red', text: 'Inactive' };
      }
    }

    return { color: 'blue', text: 'Pending' };
  }

  getNextScrapeInfo(mapping: ProductSellerMapping): string {
    if (!mapping.isActiveForScraping) {
      return 'Scraping disabled';
    }

    if (mapping.nextScrapeAt) {
      const nextScrape = new Date(mapping.nextScrapeAt);
      const now = new Date();

      if (nextScrape <= now) {
        return 'Due now';
      }

      const hoursUntilNext = Math.ceil((nextScrape.getTime() - now.getTime()) / (1000 * 60 * 60));
      return `In ${hoursUntilNext}h`;
    }

    return 'Not scheduled';
  }

  formatUrl(url: string): string {
    if (url.length > 50) {
      return url.substring(0, 47) + '...';
    }
    return url;
  }
}
