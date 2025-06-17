import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ProductDiscoveryService, ProductDiscoveryCandidate, CandidateFilterRequest, PagedResponse } from '../../services/product-discovery.service';

@Component({
  selector: 'app-candidate-list',
  templateUrl: './candidate-list.component.html',
  styleUrls: ['./candidate-list.component.css'],
  standalone: false
})
export class CandidateListComponent implements OnInit {
  candidates: ProductDiscoveryCandidate[] = [];
  isLoading = false;
  error: string | null = null;

  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  // Filtering
  filterForm: FormGroup;
  statusOptions = [
    { value: '', label: 'All Statuses' },
    { value: 'Pending', label: 'Pending' },
    { value: 'UnderReview', label: 'Under Review' },
    { value: 'Approved', label: 'Approved' },
    { value: 'Rejected', label: 'Rejected' },
    { value: 'RequiresMoreInfo', label: 'Requires More Info' }
  ];

  constructor(
    private fb: FormBuilder,
    private productDiscoveryService: ProductDiscoveryService
  ) {
    this.filterForm = this.fb.group({
      status: [''],
      searchTerm: [''],
      discoveredAfter: [''],
      discoveredBefore: ['']
    });
  }

  ngOnInit(): void {
    this.loadCandidates();

    // Auto-search on form changes with debounce
    this.filterForm.valueChanges.subscribe(() => {
      this.currentPage = 1;
      this.loadCandidates();
    });
  }

  loadCandidates(): void {
    this.isLoading = true;
    this.error = null;

    const filter: CandidateFilterRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'CreatedAt',
      sortDescending: true,
      ...this.filterForm.value
    };

    // Remove empty values
    Object.keys(filter).forEach(key => {
      if (filter[key as keyof CandidateFilterRequest] === '' || filter[key as keyof CandidateFilterRequest] === null) {
        delete filter[key as keyof CandidateFilterRequest];
      }
    });

    this.productDiscoveryService.getCandidates(filter).subscribe({
      next: (response: PagedResponse<ProductDiscoveryCandidate>) => {
        this.isLoading = false;
        if (response.isSuccess) {
          this.candidates = response.data;
          this.totalCount = response.totalCount;
          this.totalPages = response.totalPages;
        } else {
          this.error = response.message || 'Failed to load candidates';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.error = error.error?.message || 'An error occurred while loading candidates';
      }
    });
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadCandidates();
  }

  onPageSizeChange(event: Event): void {
    const pageSize = +(event.target as HTMLSelectElement).value;
    this.pageSize = pageSize;
    this.currentPage = 1;
    this.loadCandidates();
  }

  onRefresh(): void {
    this.loadCandidates();
  }

  onClearFilters(): void {
    this.filterForm.reset();
    this.currentPage = 1;
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

  getConfidenceClass(score: number): string {
    if (score >= 0.8) return 'text-success';
    if (score >= 0.6) return 'text-warning';
    return 'text-danger';
  }

  getPaginationArray(): number[] {
    const pages: number[] = [];
    const start = Math.max(1, this.currentPage - 2);
    const end = Math.min(this.totalPages, this.currentPage + 2);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  }
}
