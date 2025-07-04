import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, takeUntil } from 'rxjs';
import { 
  ProductSellerMappingDto, 
  ScraperSiteConfigurationDto,
  ProductDto
} from '../../../../shared/api/api-client';
import { 
  MappingsService,
  BulkCreateProductSellerMappingDto,
  BulkUpdateProductSellerMappingDto,
  ProductSellerMappingBulkUpdateDto
} from '../../../mappings/services/mappings.service';
import { SiteConfigsService } from '../../../site-configs/services/site-configs.service';
import { ProductMappingDialogComponent, ProductMappingDialogData } from './product-mapping-dialog/product-mapping-dialog.component';
import { ProductMappingGroup } from './product-mapping-group.model';

@Component({
  selector: 'app-product-mappings',
  templateUrl: './product-mappings.component.html',
  styleUrls: ['./product-mappings.component.scss'],
  standalone: false
})

export class ProductMappingsComponent implements OnInit, OnDestroy {
  productMappingGroups: ProductMappingGroup[] = [];
  
  /**
   * The id of the product whose seller mappings we are editing. Provided by the parent
   * `ProductFormComponent` via property binding:
   * `<app-product-mappings [productId]="productId!"></app-product-mappings>`
   */
  @Input() productId!: string;

  /**
   * Flat list of mappings for the current product. This is kept in-sync with
   * `productMappingGroups` and is used for quick array operations such as
   * push / splice / findIndex etc.
   */
  private mappings: ProductSellerMappingDto[] = [];

  /**
   * Snapshot of the mappings as last loaded from the server – used for the
   * *discard changes* functionality.
   */
  private originalMappings: ProductSellerMappingDto[] = [];

  siteConfigurations: ScraperSiteConfigurationDto[] = [];
  isLoading = false;
  isSaving = false;
  displayedColumns: string[] = ['sellerName', 'exactProductUrl', 'isActiveForScraping', 'siteConfiguration', 'actions'];
  
  private destroy$ = new Subject<void>();
  private pendingChanges: {
    create: BulkCreateProductSellerMappingDto[];
    update: BulkUpdateProductSellerMappingDto[];
    deleteIds: string[];
  } = {
    create: [],
    update: [],
    deleteIds: []
  };

  constructor(
    private mappingsService: MappingsService,
    private siteConfigsService: SiteConfigsService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadAllMappings();
    this.loadSiteConfigurations();
  }

  loadSiteConfigurations(): void {
    this.siteConfigsService.getSiteConfigs()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (configs) => {
          this.siteConfigurations = configs || [];
        },
        error: (error) => {
          console.error('Error loading site configurations:', error);
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadAllMappings(): void {
    this.isLoading = true;
    this.mappingsService.getMappings(this.productId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (mappings) => {
          this.groupMappingsByProduct(mappings);
          this.originalMappings = [...mappings];
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading mappings:', error);
          this.snackBar.open('Failed to load mappings', 'Close', { duration: 5000 });
          this.isLoading = false;
        }
      });
  }

  private groupMappingsByProduct(mappings: ProductSellerMappingDto[]): void {
    console.log(mappings)
    // Keep the flat collection updated for quick operations
    this.mappings = mappings;

    const groups: { [productName: string]: ProductSellerMappingDto[] } = {};

    for (const mapping of mappings) {
      // `canonicalProduct` was renamed to `product` in the API – adjust accordingly
      const productName = (mapping.product as ProductDto)?.name || 'Unknown Product';
      if (!groups[productName]) {
        groups[productName] = [];
      }
      groups[productName].push(mapping);
    }

    this.productMappingGroups = Object.keys(groups).map(productName => ({
      productName,
      mappings: groups[productName]
    }));
  }

  addMapping(): void {
    const dialogData: ProductMappingDialogData = {
      mapping: null,
      siteConfigurations: this.siteConfigurations,
      isEdit: false
    };

    const dialogRef = this.dialog.open(ProductMappingDialogComponent, {
      width: '600px',
      data: dialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const createDto: BulkCreateProductSellerMappingDto = {
          sellerName: result.sellerName,
          exactProductUrl: result.exactProductUrl,
          isActiveForScraping: result.isActiveForScraping ?? true,
          scrapingFrequencyOverride: result.scrapingFrequencyOverride,
          siteConfigId: result.siteConfigId
        };
        
        this.pendingChanges.create.push(createDto);
        
        // Add temporary mapping to display
        const tempMapping = new ProductSellerMappingDto();
        tempMapping.mappingId = `temp-${Date.now()}`;
        tempMapping.sellerName = result.sellerName;
        tempMapping.exactProductUrl = result.exactProductUrl;
        tempMapping.isActiveForScraping = result.isActiveForScraping ?? true;
        tempMapping.scrapingFrequencyOverride = result.scrapingFrequencyOverride;
        tempMapping.siteConfigId = result.siteConfigId;
        tempMapping.createdAt = new Date();
        tempMapping.updatedAt = new Date();
        tempMapping.consecutiveFailureCount = 0;
        
        this.mappings.push(tempMapping);
        this.groupMappingsByProduct(this.mappings);
        this.snackBar.open('Mapping added (save to persist)', 'Close', { duration: 3000 });
      }
    });
  }

  editMapping(mapping: ProductSellerMappingDto): void {
    const dialogData: ProductMappingDialogData = {
      mapping: mapping,
      siteConfigurations: this.siteConfigurations,
      isEdit: true
    };

    const dialogRef = this.dialog.open(ProductMappingDialogComponent, {
      width: '600px',
      data: dialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result && mapping.mappingId) {
        // Only track updates for existing mappings (not temp ones)
        if (!mapping.mappingId.startsWith('temp-')) {
          const updateDto: BulkUpdateProductSellerMappingDto = {
            mappingId: mapping.mappingId,
            sellerName: result.sellerName,
            exactProductUrl: result.exactProductUrl,
            isActiveForScraping: result.isActiveForScraping,
            scrapingFrequencyOverride: result.scrapingFrequencyOverride,
            siteConfigId: result.siteConfigId
          };
          
          // Remove any existing update for this mapping
          const existingUpdateIndex = this.pendingChanges.update.findIndex(u => u.mappingId === mapping.mappingId);
          if (existingUpdateIndex >= 0) {
            this.pendingChanges.update[existingUpdateIndex] = updateDto;
          } else {
            this.pendingChanges.update.push(updateDto);
          }
        }
        
        // Update the mapping directly in the array
        Object.assign(mapping, {
          sellerName: result.sellerName,
          exactProductUrl: result.exactProductUrl,
          isActiveForScraping: result.isActiveForScraping,
          scrapingFrequencyOverride: result.scrapingFrequencyOverride,
          siteConfigId: result.siteConfigId,
          updatedAt: new Date()
        });
        
        this.groupMappingsByProduct(this.mappings);
        this.snackBar.open('Mapping updated (save to persist)', 'Close', { duration: 3000 });
      }
    });
  }

  deleteMapping(mapping: ProductSellerMappingDto): void {
    if (confirm('Are you sure you want to delete this mapping?')) {
      if (mapping.mappingId && !mapping.mappingId.startsWith('temp-')) {
        // Real mapping - add to delete list
        this.pendingChanges.deleteIds.push(mapping.mappingId);
      } else {
        // Temp mapping - remove from create list
        const createIndex = this.pendingChanges.create.findIndex(c => 
          c.sellerName === mapping.sellerName && c.exactProductUrl === mapping.exactProductUrl
        );
        if (createIndex >= 0) {
          this.pendingChanges.create.splice(createIndex, 1);
        }
      }
      
      // Remove from display
      const index = this.mappings.findIndex(m => m.mappingId === mapping.mappingId);
      if (index >= 0) {
        this.mappings.splice(index, 1);
      }
      
      this.groupMappingsByProduct(this.mappings);
      this.snackBar.open('Mapping deleted (save to persist)', 'Close', { duration: 3000 });
    }
  }

  saveChanges(): void {
    if (!this.hasPendingChanges()) {
      this.snackBar.open('No changes to save', 'Close', { duration: 3000 });
      return;
    }
    
    this.isSaving = true;
    
    const bulkUpdateDto: ProductSellerMappingBulkUpdateDto = {
      create: this.pendingChanges.create,
      update: this.pendingChanges.update,
      deleteIds: this.pendingChanges.deleteIds
    };
    
    this.mappingsService.bulkUpdateProductMappings(this.productId, bulkUpdateDto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedMappings) => {
          this.mappings = [...(updatedMappings || [])];
          this.originalMappings = [...(updatedMappings || [])];
          this.groupMappingsByProduct(this.mappings);
          this.clearPendingChanges();
          this.isSaving = false;
          this.snackBar.open('All changes saved successfully', 'Close', { duration: 3000 });
        },
        error: (error) => {
          console.error('Error saving changes:', error);
          this.snackBar.open('Failed to save changes', 'Close', { duration: 5000 });
          this.isSaving = false;
        }
      });
  }

  discardChanges(): void {
    this.mappings = [...this.originalMappings];
    this.groupMappingsByProduct(this.mappings);
    this.clearPendingChanges();
    this.snackBar.open('Changes discarded', 'Close', { duration: 3000 });
  }

  hasPendingChanges(): boolean {
    return this.pendingChanges.create.length > 0 || 
           this.pendingChanges.update.length > 0 || 
           this.pendingChanges.deleteIds.length > 0;
  }

  private clearPendingChanges(): void {
    this.pendingChanges = {
      create: [],
      update: [],
      deleteIds: []
    };
  }

  getSiteConfigurationName(siteConfigId?: string): string {
    if (!siteConfigId) return 'Auto-detect';
    const config = this.siteConfigurations.find(c => c.siteConfigId === siteConfigId);
    return config?.siteDomain || 'Unknown';
  }
}
