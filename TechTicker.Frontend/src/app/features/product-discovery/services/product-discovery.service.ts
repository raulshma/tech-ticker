import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  TechTickerApiClient,
  AnalyzeUrlRequest,
  BulkAnalyzeRequest,
  ApprovalRequest,
  RejectionRequest,
  DiscoveryStatus,
  ProductDiscoveryCandidateDto,
  ProductDiscoveryCandidateDtoApiResponse,
  ProductDiscoveryCandidateDtoPagedResponse,
  DiscoveryResult,
  WorkflowAction
} from '../../../shared/api/api-client';

// Export types for use in components
export type ProductDiscoveryCandidate = ProductDiscoveryCandidateDto;
export type PagedResponse<T> = {
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
};
export { DiscoveryResult };

export interface CandidateFilterRequest {
  status?: DiscoveryStatus;
  categoryId?: string;
  discoveredByUserId?: string;
  discoveredAfter?: Date;
  discoveredBefore?: Date;
  searchTerm?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface ApprovalDecision {
  action: WorkflowAction;
  comments?: string;
  modifications?: { [key: string]: any };
  createProduct?: boolean;
  categoryOverride?: string;
  productNameOverride?: string;
}

export interface RejectionDecision {
  reason: string;
  comments?: string;
  blockDomain?: boolean;
  useForTraining?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ProductDiscoveryService {

  constructor(private apiClient: TechTickerApiClient) { }

  analyzeUrl(request: AnalyzeUrlRequest): Observable<DiscoveryResult> {
    return this.apiClient.analyzeUrl(request);
  }

  bulkAnalyzeUrls(request: BulkAnalyzeRequest): Observable<DiscoveryResult> {
    return this.apiClient.bulkAnalyze(request);
  }

  getCandidates(filter: CandidateFilterRequest): Observable<ProductDiscoveryCandidateDtoPagedResponse> {
    return this.apiClient.candidates(
      filter.status,
      filter.categoryId,
      filter.discoveredByUserId,
      filter.discoveredAfter,
      filter.discoveredBefore,
      filter.searchTerm,
      filter.page,
      filter.pageSize,
      filter.sortBy,
      filter.sortDescending
    );
  }

  getCandidate(candidateId: string): Observable<ProductDiscoveryCandidateDtoApiResponse> {
    return this.apiClient.candidates2(candidateId);
  }

  approveCandidate(candidateId: string, decision: ApprovalDecision): Observable<any> {
    const request = new ApprovalRequest({
      action: decision.action,
      comments: decision.comments,
      modifications: decision.modifications,
      createProduct: decision.createProduct,
      categoryOverride: decision.categoryOverride,
      productNameOverride: decision.productNameOverride
    });

    return this.apiClient.approve(candidateId, request).pipe(
      map(response => response.data)
    );
  }

  rejectCandidate(candidateId: string, decision: RejectionDecision): Observable<any> {
    const request = new RejectionRequest({
      reason: decision.reason,
      comments: decision.comments,
      blockDomain: decision.blockDomain,
      useForTraining: decision.useForTraining
    });

    return this.apiClient.reject(candidateId, request).pipe(
      map(response => response.data)
    );
  }

  getSimilarProducts(candidateId: string, limit?: number): Observable<any[]> {
    return this.apiClient.similarProducts(candidateId, limit).pipe(
      map(response => response.data || [])
    );
  }

  retryFailedCandidates(candidateIds: string[]): Observable<DiscoveryResult> {
    return this.apiClient.retry(candidateIds).pipe(
      map(response => response.data!)
    );
  }

  getStatistics(startDate: Date, endDate: Date, grouping?: string): Observable<any> {
    return this.apiClient.statistics(startDate, endDate, grouping).pipe(
      map(response => response.data)
    );
  }
}
