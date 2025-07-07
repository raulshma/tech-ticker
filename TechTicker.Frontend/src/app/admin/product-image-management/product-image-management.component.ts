import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';
import { TechTickerApiClient, ProductWithCurrentPricesDto, ImageDto } from '../../shared/api/api-client';
import { ImageApiExtension } from '../../shared/api/image-api-extension';

@Component({
  selector: 'app-product-image-management',
  templateUrl: './product-image-management.component.html',
  styleUrls: ['./product-image-management.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatSnackBarModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatCheckboxModule,
    MatChipsModule,
    MatTooltipModule
  ]
})
export class ProductImageManagementComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Selected product
  selectedProduct: ProductWithCurrentPricesDto | null = null;
  productImages: ImageDto[] = [];

  // Product search
  searchControl = new FormControl('');
  searchResults: ProductWithCurrentPricesDto[] = [];
  isSearching = false;

  // Image management state
  isLoadingImages = false;
  isUploadingImages = false;
  selectedImages: Set<string> = new Set();

  // UI state
  showUploadArea = false;
  showBulkActions = false;

  constructor(
    private apiClient: TechTickerApiClient,
    private imageApiExtension: ImageApiExtension,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.setupProductSearch();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupProductSearch(): void {
    this.searchControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(searchTerm => {
        if (searchTerm && searchTerm.length >= 2) {
          this.searchProducts(searchTerm);
        } else {
          this.searchResults = [];
        }
      });
  }

  private async searchProducts(searchTerm: string): Promise<void> {
    try {
      this.isSearching = true;
      const response = await this.apiClient.getProductsCatalog(undefined, searchTerm, 1, 20).toPromise();
      
      if (response?.data) {
        this.searchResults = response.data;
      } else {
        this.searchResults = [];
      }
    } catch (error) {
      console.error('Error searching products:', error);
      this.showErrorMessage('Failed to search products');
      this.searchResults = [];
    } finally {
      this.isSearching = false;
    }
  }

  async selectProduct(product: ProductWithCurrentPricesDto): Promise<void> {
    this.selectedProduct = product;
    this.searchResults = [];
    this.searchControl.setValue('');
    this.selectedImages.clear();
    this.showBulkActions = false;
    
    await this.loadProductImages();
  }

  private async loadProductImages(): Promise<void> {
    if (!this.selectedProduct?.productId) return;

    try {
      this.isLoadingImages = true;
      const response = await this.imageApiExtension.getProductImages(this.selectedProduct.productId).toPromise();
      
      if (response?.data) {
        this.productImages = response.data;
      } else {
        this.productImages = [];
      }
    } catch (error) {
      console.error('Error loading product images:', error);
      this.showErrorMessage('Failed to load product images');
      this.productImages = [];
    } finally {
      this.isLoadingImages = false;
    }
  }

  onFileInputChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    if (target?.files) {
      this.onFilesSelected(target.files);
    }
  }

  onFilesSelected(files: FileList): void {
    if (!this.selectedProduct?.productId) {
      this.showErrorMessage('Please select a product first');
      return;
    }

    this.uploadImages(files);
  }

  private async uploadImages(files: FileList): Promise<void> {
    if (!this.selectedProduct?.productId) return;

    try {
      this.isUploadingImages = true;
      const imageUploadDtos: any[] = [];

      for (let i = 0; i < files.length; i++) {
        const file = files[i];
        // Convert file to base64 or handle as needed by the API
        const reader = new FileReader();
        reader.readAsDataURL(file);
        reader.onload = () => {
          imageUploadDtos.push({
            file: reader.result as string,
            altText: file.name,
            description: `Uploaded image: ${file.name}`
          });
        };
      }

      // Wait for all files to be processed
      await new Promise(resolve => {
        const checkComplete = () => {
          if (imageUploadDtos.length === files.length) {
            resolve(undefined);
          } else {
            setTimeout(checkComplete, 100);
          }
        };
        checkComplete();
      });

      const response = await this.imageApiExtension.uploadImages(this.selectedProduct.productId, imageUploadDtos).toPromise();
      
      if (response?.data) {
        this.showSuccessMessage(`Successfully uploaded ${response.data.length} images`);
        await this.loadProductImages();
      }
    } catch (error) {
      console.error('Error uploading images:', error);
      this.showErrorMessage('Failed to upload images');
    } finally {
      this.isUploadingImages = false;
    }
  }

  async deleteImage(imageUrl: string): Promise<void> {
    if (!this.selectedProduct?.productId) return;

    try {
      await this.imageApiExtension.deleteImage(this.selectedProduct.productId, imageUrl).toPromise();
      
      this.showSuccessMessage('Image deleted successfully');
      await this.loadProductImages();
      this.selectedImages.delete(imageUrl);
    } catch (error) {
      console.error('Error deleting image:', error);
      this.showErrorMessage('Failed to delete image');
    }
  }

  async setPrimaryImage(imageUrl: string): Promise<void> {
    if (!this.selectedProduct?.productId) return;

    try {
      await this.imageApiExtension.setPrimaryImage(this.selectedProduct.productId, imageUrl).toPromise();
      
      this.showSuccessMessage('Primary image updated successfully');
      await this.loadProductImages();
    } catch (error) {
      console.error('Error setting primary image:', error);
      this.showErrorMessage('Failed to set primary image');
    }
  }

  async reorderImages(newOrder: string[]): Promise<void> {
    if (!this.selectedProduct?.productId) return;

    try {
      await this.imageApiExtension.reorderImages(this.selectedProduct.productId, newOrder).toPromise();
      
      this.showSuccessMessage('Images reordered successfully');
      await this.loadProductImages();
    } catch (error) {
      console.error('Error reordering images:', error);
      this.showErrorMessage('Failed to reorder images');
    }
  }

  toggleImageSelection(imageUrl: string): void {
    if (this.selectedImages.has(imageUrl)) {
      this.selectedImages.delete(imageUrl);
    } else {
      this.selectedImages.add(imageUrl);
    }
    
    this.showBulkActions = this.selectedImages.size > 0;
  }

  selectAllImages(): void {
    this.productImages.forEach(image => this.selectedImages.add(image.url!));
    this.showBulkActions = this.selectedImages.size > 0;
  }

  clearSelection(): void {
    this.selectedImages.clear();
    this.showBulkActions = false;
  }

  async deleteSelectedImages(): Promise<void> {
    if (!this.selectedProduct?.productId || this.selectedImages.size === 0) return;

    try {
      const imageUrls = Array.from(this.selectedImages);
      const response = await this.imageApiExtension.bulkDeleteImages(
        this.selectedProduct.productId, 
        imageUrls
      ).toPromise();
      
      if (response?.data) {
        const result = response.data;
        this.showSuccessMessage(
          `Successfully deleted ${result.successfulOperations} of ${result.totalRequested} images`
        );
        
        if (result.failedOperations && result.failedOperations > 0) {
          console.warn('Some images failed to delete:', result.errors);
        }
      }

      await this.loadProductImages();
      this.clearSelection();
    } catch (error) {
      console.error('Error bulk deleting images:', error);
      this.showErrorMessage('Failed to delete selected images');
    }
  }

  toggleUploadArea(): void {
    this.showUploadArea = !this.showUploadArea;
  }

  clearProductSelection(): void {
    this.selectedProduct = null;
    this.productImages = [];
    this.selectedImages.clear();
    this.showBulkActions = false;
    this.showUploadArea = false;
  }

  getImageDisplayUrl(imageUrl: string): string {
    // Convert relative URL to full URL for display
    if (imageUrl.startsWith('images/')) {
      return `/api/${imageUrl}`;
    }
    return imageUrl;
  }

  private showSuccessMessage(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: ['success-snackbar']
    });
  }

  private showErrorMessage(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }

  // Template helper methods
  get hasSelectedProduct(): boolean {
    return this.selectedProduct !== null;
  }

  get hasImages(): boolean {
    return this.productImages.length > 0;
  }

  get selectedImageCount(): number {
    return this.selectedImages.size;
  }

  get isImageSelected(): (imageUrl: string) => boolean {
    return (imageUrl: string) => this.selectedImages.has(imageUrl);
  }

  trackByImageUrl(index: number, image: ImageDto): string {
    return image.url || index.toString();
  }

  onImageError(event: Event): void {
    const target = event.target as HTMLImageElement;
    if (target) {
      target.src = '/assets/images/image-error.png';
    }
  }
} 