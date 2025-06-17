import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ProductDiscoveryService, ProductDiscoveryCandidate } from '../../services/product-discovery.service';

@Component({
  selector: 'app-candidate-detail',
  templateUrl: './candidate-detail.component.html',
  styleUrls: ['./candidate-detail.component.css'],
  standalone: false
})
export class CandidateDetailComponent implements OnInit {
  candidate: ProductDiscoveryCandidate | null = null;
  isLoading = false;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private productDiscoveryService: ProductDiscoveryService
  ) { }

  ngOnInit(): void {
    const candidateId = this.route.snapshot.paramMap.get('id');
    if (candidateId) {
      this.loadCandidate(candidateId);
    }
  }

  loadCandidate(candidateId: string): void {
    this.isLoading = true;
    this.error = null;

    this.productDiscoveryService.getCandidate(candidateId).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.isSuccess) {
          this.candidate = response.data;
        } else {
          this.error = response.message || 'Failed to load candidate';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.error = error.error?.message || 'An error occurred while loading the candidate';
      }
    });
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

  approveCandidate(): void {
    // TODO: Implement approval logic
    console.log('Approve candidate');
  }

  rejectCandidate(): void {
    // TODO: Implement rejection logic
    console.log('Reject candidate');
  }

  requestModification(): void {
    // TODO: Implement modification request logic
    console.log('Request modification');
  }
}
