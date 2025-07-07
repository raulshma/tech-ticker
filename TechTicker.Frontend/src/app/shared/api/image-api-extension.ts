import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { 
  TechTickerApiClient, 
  ImageDtoListApiResponse, 
  BooleanApiResponse, 
  BulkImageOperationResultDtoApiResponse, 
  ImageMetadataDtoApiResponse,
  ObjectApiResponse,
  SetPrimaryImageDto,
  ImageReorderDto,
  BulkImageOperationDto,
  ImageUploadDto
} from './api-client';

@Injectable({
  providedIn: 'root'
})
export class ImageApiExtension {

  constructor(private apiClient: TechTickerApiClient) {}

  /**
   * Upload multiple images for a product
   */
  uploadImages(productId: string, images: ImageUploadDto[]): Observable<ImageDtoListApiResponse> {
    return this.apiClient.uploadImages(productId, images);
  }

  /**
   * Get all images for a product
   */
  getProductImages(productId: string): Observable<ImageDtoListApiResponse> {
    return this.apiClient.getProductImages(productId);
  }

  /**
   * Delete a specific image from a product
   */
  deleteImage(productId: string, imageUrl: string): Observable<BooleanApiResponse> {
    return this.apiClient.deleteImage(productId, imageUrl);
  }

  /**
   * Set a specific image as the primary image for a product
   */
  setPrimaryImage(productId: string, imageUrl: string): Observable<BooleanApiResponse> {
    const request = new SetPrimaryImageDto({ imageUrl });
    return this.apiClient.setPrimaryImage(productId, request);
  }

  /**
   * Reorder images for a product
   */
  reorderImages(productId: string, imageUrls: string[]): Observable<BooleanApiResponse> {
    const request = new ImageReorderDto({ imageUrls });
    return this.apiClient.reorderImages(productId, request);
  }

  /**
   * Delete multiple images from a product
   */
  bulkDeleteImages(productId: string, imageUrls: string[]): Observable<BulkImageOperationResultDtoApiResponse> {
    const request = new BulkImageOperationDto({ imageUrls });
    return this.apiClient.bulkDeleteImages(productId, request);
  }

  /**
   * Get image metadata including file size and dimensions
   */
  getImageMetadata(imageUrl: string): Observable<ImageMetadataDtoApiResponse> {
    return this.apiClient.getImageMetadata(imageUrl);
  }

  /**
   * Health check endpoint for image management service
   */
  getHealthStatus(): Observable<ObjectApiResponse> {
    return this.apiClient.getHealthStatus();
  }
} 