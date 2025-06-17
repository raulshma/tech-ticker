import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ProductDiscoveryService, DiscoveryResult } from '../../services/product-discovery.service';
import { AnalyzeUrlRequest } from '../../../../shared/api/api-client';

@Component({
  selector: 'app-url-analysis',
  templateUrl: './url-analysis.component.html',
  styleUrls: ['./url-analysis.component.css'],
  standalone: false
})
export class UrlAnalysisComponent implements OnInit {
  analysisForm: FormGroup;
  isAnalyzing = false;
  result: DiscoveryResult | null = null;
  error: string | null = null;

  constructor(
    private fb: FormBuilder,
    private productDiscoveryService: ProductDiscoveryService
  ) {
    this.analysisForm = this.fb.group({
      url: ['', [Validators.required, Validators.pattern(/^https?:\/\/.+/)]]
    });
  }

  ngOnInit(): void {}

  onAnalyze(): void {
    if (this.analysisForm.valid) {
      this.isAnalyzing = true;
      this.error = null;
      this.result = null;

      const request = new AnalyzeUrlRequest({
        url: this.analysisForm.get('url')?.value
      });

      this.productDiscoveryService.analyzeUrl(request).subscribe({
        next: (response) => {
          this.isAnalyzing = false;
          this.result = response;
        },
        error: (error) => {
          this.isAnalyzing = false;
          this.error = error.error?.message || 'An error occurred during analysis';
        }
      });
    }
  }

  onReset(): void {
    this.analysisForm.reset();
    this.result = null;
    this.error = null;
  }

  getStatusBadgeClass(status: any): string {
    const statusStr = status?.toString() || '';
    switch (statusStr) {
      case 'Pending': return 'badge-warning';
      case 'UnderReview': return 'badge-info';
      case 'Approved': return 'badge-success';
      case 'Rejected': return 'badge-danger';
      case 'RequiresMoreInfo': return 'badge-secondary';
      default: return 'badge-light';
    }
  }

  // Helper methods for template
  getMetadataValue(metadata: any, key: string): any {
    return metadata?.[key] || 0;
  }

  getMetadataArray(metadata: any, key: string): any[] {
    return metadata?.[key] || [];
  }

  getConfidenceScore(score: number | undefined): number {
    return score || 0;
  }

  getSimilarityScore(score: number | undefined): number {
    return score || 0;
  }

  getProductName(product: any): string {
    return product?.name || 'Unknown Product';
  }
}
