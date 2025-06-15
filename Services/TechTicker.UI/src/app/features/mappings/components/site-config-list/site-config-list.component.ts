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
import { finalize } from 'rxjs/operators';
import { SiteConfiguration, SiteConfigurationService, SiteConfigurationSearchParams } from '../../services/site-configuration.service';

@Component({
  selector: 'app-site-config-list',
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
    NzToolTipModule
  ],
  templateUrl: './site-config-list.component.html',
  styleUrls: ['./site-config-list.component.css']
})
export class SiteConfigListComponent implements OnInit {
  siteConfigs: SiteConfiguration[] = [];
  loading = false;
  total = 0;
  pageIndex = 1;
  pageSize = 10;
  sortField: string | null = null;
  sortOrder: string | null = null;
  searchValue = '';
  domainFilter = '';
  activeFilter: boolean | null = null;

  constructor(
    private siteConfigService: SiteConfigurationService,
    private router: Router,
    private message: NzMessageService,
    private modal: NzModalService
  ) {}

  ngOnInit(): void {
    this.loadSiteConfigs();
  }

  loadSiteConfigs(): void {
    this.loading = true;

    const params: SiteConfigurationSearchParams = {
      page: this.pageIndex,
      pageSize: this.pageSize,
      sortBy: this.sortField || undefined,
      sortOrder: this.sortOrder === 'ascend' ? 'asc' : this.sortOrder === 'descend' ? 'desc' : undefined
    };

    if (this.searchValue) {
      params.name = this.searchValue;
    }

    if (this.domainFilter) {
      params.domain = this.domainFilter;
    }

    if (this.activeFilter !== null) {
      params.isActive = this.activeFilter;
    }

    this.siteConfigService.getSiteConfigurations(params)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (response) => {
          this.siteConfigs = response.items;
          this.total = response.total;
        },
        error: (error) => {
          console.error('Error loading site configurations:', error);
          this.message.error('Failed to load site configurations');
        }
      });
  }

  onQueryParamsChange(params: any): void {
    this.pageIndex = params.pageIndex;
    this.pageSize = params.pageSize;
    this.sortField = params.sort?.[0]?.key || null;
    this.sortOrder = params.sort?.[0]?.value || null;
    this.loadSiteConfigs();
  }

  onSearch(): void {
    this.pageIndex = 1;
    this.loadSiteConfigs();
  }

  onResetFilters(): void {
    this.searchValue = '';
    this.domainFilter = '';
    this.activeFilter = null;
    this.pageIndex = 1;
    this.loadSiteConfigs();
  }

  createSiteConfig(): void {
    this.router.navigate(['/mappings/site-configs/create']);
  }

  viewSiteConfig(id: string): void {
    this.router.navigate(['/mappings/site-configs', id]);
  }

  editSiteConfig(id: string): void {
    this.router.navigate(['/mappings/site-configs', id, 'edit']);
  }

  deleteSiteConfig(siteConfig: SiteConfiguration): void {
    this.modal.confirm({
      nzTitle: 'Delete Site Configuration',
      nzContent: `Are you sure you want to delete the site configuration for "${siteConfig.domain}"? This action cannot be undone and may affect existing product-seller mappings.`,
      nzOkText: 'Delete',
      nzOkType: 'primary',
      nzOkDanger: true,
      nzCancelText: 'Cancel',
      nzOnOk: () => {
        this.loading = true;
        this.siteConfigService.deleteSiteConfiguration(siteConfig.id)
          .pipe(finalize(() => this.loading = false))
          .subscribe({
            next: () => {
              this.message.success('Site configuration deleted successfully');
              this.loadSiteConfigs();
            },
            error: (error) => {
              console.error('Error deleting site configuration:', error);
              this.message.error('Failed to delete site configuration');
            }
          });
      }
    });
  }

  toggleStatus(siteConfig: SiteConfiguration): void {
    const newStatus = !siteConfig.isActive;
    const action = newStatus ? 'activate' : 'deactivate';

    this.modal.confirm({
      nzTitle: `${action.charAt(0).toUpperCase() + action.slice(1)} Site Configuration`,
      nzContent: `Are you sure you want to ${action} the site configuration for "${siteConfig.domain}"?`,
      nzOkText: action.charAt(0).toUpperCase() + action.slice(1),
      nzOkType: 'primary',
      nzCancelText: 'Cancel',
      nzOnOk: () => {
        this.siteConfigService.updateSiteConfiguration(siteConfig.id, { isActive: newStatus })
          .subscribe({
            next: () => {
              this.message.success(`Site configuration ${action}d successfully`);
              this.loadSiteConfigs();
            },
            error: (error) => {
              console.error(`Error ${action}ing site configuration:`, error);
              this.message.error(`Failed to ${action} site configuration`);
            }
          });
      }
    });
  }

  getStatusTag(isActive: boolean): { color: string; text: string } {
    return isActive
      ? { color: 'green', text: 'Active' }
      : { color: 'red', text: 'Inactive' };
  }
}
