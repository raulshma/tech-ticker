import { Component, Input, OnInit } from '@angular/core';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-image-gallery',
  templateUrl: './image-gallery.component.html',
  styleUrls: ['./image-gallery.component.scss'],
  standalone: false
})
export class ImageGalleryComponent implements OnInit {
  @Input() primaryImageUrl?: string | null;
  @Input() additionalImageUrls?: string[] | null;
  @Input() altText: string = 'Product image';
  @Input() showThumbnails: boolean = true;
  @Input() maxHeight: string = '400px';
  @Input() maxWidth: string = '100%';

  selectedImageUrl?: string;
  allImageUrls: string[] = [];
  selectedIndex: number = 0;
  imageLoadErrors: Set<string> = new Set();

  ngOnInit(): void {
    this.buildImageList();
    this.selectedImageUrl = this.allImageUrls[0];
  }

  ngOnChanges(): void {
    this.buildImageList();
    this.selectedImageUrl = this.allImageUrls[0];
    this.selectedIndex = 0;
    this.imageLoadErrors.clear();
  }

  private buildImageList(): void {
    this.allImageUrls = [];

    if (this.primaryImageUrl) {
      this.allImageUrls.push(this.primaryImageUrl);
    }

    if (this.additionalImageUrls && this.additionalImageUrls.length > 0) {
      this.allImageUrls.push(...this.additionalImageUrls);
    }
  }

  selectImage(imageUrl: string, index: number, event?: Event): void {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    this.selectedImageUrl = imageUrl;
    this.selectedIndex = index;
  }

  onImageError(imageUrl: string): void {
    this.imageLoadErrors.add(imageUrl);
  }

  isImageValid(imageUrl: string): boolean {
    return !this.imageLoadErrors.has(imageUrl);
  }

  getImageUrl(imageUrl: string): string {
    // Convert relative paths to absolute URLs
    if (imageUrl && !imageUrl.startsWith('http')) {
      // Use the API base URL from environment
      const baseUrl = this.getApiBaseUrl();
      return `${baseUrl}/${imageUrl}`;
    }
    return imageUrl;
  }

  private getApiBaseUrl(): string {
    // Get the API base URL from environment, fallback to current location
    return environment.apiUrl || window.location.origin;
  }

  hasImages(): boolean {
    return this.allImageUrls.length > 0;
  }

  getValidImages(): string[] {
    return this.allImageUrls.filter(url => this.isImageValid(url));
  }

  previousImage(event?: Event): void {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    if (this.allImageUrls.length > 1) {
      this.selectedIndex = this.selectedIndex > 0 ? this.selectedIndex - 1 : this.allImageUrls.length - 1;
      this.selectedImageUrl = this.allImageUrls[this.selectedIndex];
    }
  }

  nextImage(event?: Event): void {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    if (this.allImageUrls.length > 1) {
      this.selectedIndex = this.selectedIndex < this.allImageUrls.length - 1 ? this.selectedIndex + 1 : 0;
      this.selectedImageUrl = this.allImageUrls[this.selectedIndex];
    }
  }
}
