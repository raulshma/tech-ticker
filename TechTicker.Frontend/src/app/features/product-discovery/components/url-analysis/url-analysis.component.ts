import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ProductDiscoveryService, DiscoveryResult } from '../../services/product-discovery.service';

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

      const request = {
        url: this.analysisForm.get('url')?.value
      };

      this.productDiscoveryService.analyzeUrl(request).subscribe({
        next: (response) => {
          this.isAnalyzing = false;
          if (response.isSuccess) {
            this.result = response.data;
          } else {
            this.error = response.message || 'Analysis failed';
          }
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

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Pending': return 'badge-warning';
      case 'UnderReview': return 'badge-info';
      case 'Approved': return 'badge-success';
      case 'Rejected': return 'badge-danger';
      case 'RequiresMoreInfo': return 'badge-secondary';
      default: return 'badge-light';
    }
  }
}
