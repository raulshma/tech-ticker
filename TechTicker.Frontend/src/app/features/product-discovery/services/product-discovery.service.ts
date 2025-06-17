import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface ProductDiscoveryCandidate {
  candidateId: string;
  sourceUrl: string;
  extractedProductName: string;
  extractedManufacturer?: string;
  extractedModelNumber?: string;
  extractedPrice?: number;
  extractedImageUrl?: string;
  extractedDescription?: string;
  extractedSpecifications?: { [key: string]: any };
  suggestedCategoryId?: string;
  categoryConfidenceScore: number;
  similarProductId?: string;
  similarityScore: number;
  discoveryMethod: string;
  discoveredByUserId?: string;
  discoveredAt: string;
  status: 'Pending' | 'UnderReview' | 'Approved' | 'Rejected' | 'RequiresMoreInfo';
  rejectionReason?: string;
  createdAt: string;
  updatedAt: string;
  suggestedCategory?: any;
  similarProduct?: any;
  discoveredByUser?: any;
}

export interface DiscoveryResult {
  isSuccess: boolean;
  errorMessage?: string;
  candidates: ProductDiscoveryCandidate[];
  metadata: {
    processedUrls: number;
    successfulExtractions: number;
    failedExtractions: number;
    processingTime: string;
    warnings: string[];
    errors: string[];
  };
}

export interface AnalyzeUrlRequest {
  url: string;
  userId?: string;
}

export interface BulkAnalyzeRequest {
  urls: string[];
  batchSize?: number;
  discoveryMethod?: string;
  userId?: string;
}

export interface CandidateFilterRequest {
  status?: string;
  categoryId?: string;
  discoveredByUserId?: string;
  discoveredAfter?: string;
  discoveredBefore?: string;
  searchTerm?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface ApprovalDecision {
  action: 'Approve' | 'Reject' | 'RequestModification' | 'ApproveWithModifications';
  comments?: string;
  modifications?: { [key: string]: any };
}

export interface ApiResponse<T> {
  data: T;
  message: string;
  isSuccess: boolean;
  correlationId: string;
}

export interface PagedResponse<T> {
  data: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  isSuccess: boolean;
  message: string;
  correlationId: string;
}

@Injectable({
  providedIn: 'root'
})
export class ProductDiscoveryService {
  private readonly baseUrl = `${environment.apiUrl}/api/ProductDiscovery`;

  constructor(private http: HttpClient) { }

  analyzeUrl(request: AnalyzeUrlRequest): Observable<ApiResponse<DiscoveryResult>> {
    return this.http.post<ApiResponse<DiscoveryResult>>(`${this.baseUrl}/analyze-url`, request);
  }

  bulkAnalyzeUrls(request: BulkAnalyzeRequest): Observable<ApiResponse<DiscoveryResult>> {
    return this.http.post<ApiResponse<DiscoveryResult>>(`${this.baseUrl}/bulk-analyze`, request);
  }

  getCandidates(filter: CandidateFilterRequest): Observable<PagedResponse<ProductDiscoveryCandidate>> {
    let params = new HttpParams();
    
    if (filter.status) params = params.set('status', filter.status);
    if (filter.categoryId) params = params.set('categoryId', filter.categoryId);
    if (filter.discoveredByUserId) params = params.set('discoveredByUserId', filter.discoveredByUserId);
    if (filter.discoveredAfter) params = params.set('discoveredAfter', filter.discoveredAfter);
    if (filter.discoveredBefore) params = params.set('discoveredBefore', filter.discoveredBefore);
    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    if (filter.page) params = params.set('page', filter.page.toString());
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    if (filter.sortBy) params = params.set('sortBy', filter.sortBy);
    if (filter.sortDescending !== undefined) params = params.set('sortDescending', filter.sortDescending.toString());

    return this.http.get<PagedResponse<ProductDiscoveryCandidate>>(`${this.baseUrl}/candidates`, { params });
  }

  getCandidate(candidateId: string): Observable<ApiResponse<ProductDiscoveryCandidate>> {
    return this.http.get<ApiResponse<ProductDiscoveryCandidate>>(`${this.baseUrl}/candidates/${candidateId}`);
  }

  approveCandidate(candidateId: string, decision: ApprovalDecision): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/candidates/${candidateId}/approve`, decision);
  }

  rejectCandidate(candidateId: string, decision: ApprovalDecision): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/candidates/${candidateId}/reject`, decision);
  }

  getSimilarProducts(candidateId: string): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(`${this.baseUrl}/similar-products/${candidateId}`);
  }
}
