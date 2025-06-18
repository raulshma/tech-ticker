import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatBadgeModule } from '@angular/material/badge';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';

import { ProductDiscoveryRoutingModule } from './product-discovery-routing.module';
import { SharedModule } from '../../shared/shared.module';

// Components
import { ProductDiscoveryComponent } from './product-discovery.component';
import { UrlAnalysisComponent } from './components/url-analysis/url-analysis.component';
import { CandidateListComponent } from './components/candidate-list/candidate-list.component';
import { CandidateDetailComponent } from './components/candidate-detail/candidate-detail.component';
import { BulkAnalysisComponent } from './components/bulk-analysis/bulk-analysis.component';
import { ApprovalWorkflowComponent } from './components/approval-workflow/approval-workflow.component';

// Services
import { ProductDiscoveryService } from './services/product-discovery.service';

@NgModule({
  declarations: [
    ProductDiscoveryComponent,
    UrlAnalysisComponent,
    CandidateListComponent,
    CandidateDetailComponent,
    BulkAnalysisComponent,
    ApprovalWorkflowComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    RouterModule,
    ProductDiscoveryRoutingModule,
    SharedModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatBadgeModule,
    MatChipsModule,
    MatTooltipModule
  ],
  providers: [
    ProductDiscoveryService
  ]
})
export class ProductDiscoveryModule { }
