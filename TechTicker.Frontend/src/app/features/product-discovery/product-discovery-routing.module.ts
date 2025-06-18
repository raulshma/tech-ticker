import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { ProductDiscoveryComponent } from './product-discovery.component';
import { UrlAnalysisComponent } from './components/url-analysis/url-analysis.component';
import { CandidateListComponent } from './components/candidate-list/candidate-list.component';
import { CandidateDetailComponent } from './components/candidate-detail/candidate-detail.component';
import { BulkAnalysisComponent } from './components/bulk-analysis/bulk-analysis.component';
import { ApprovalWorkflowComponent } from './components/approval-workflow/approval-workflow.component';

const routes: Routes = [
  {
    path: '',
    component: ProductDiscoveryComponent,
    children: [
      { path: '', redirectTo: 'candidates', pathMatch: 'full' },
      { path: 'analyze', component: UrlAnalysisComponent },
      { path: 'bulk-analyze', component: BulkAnalysisComponent },
      { path: 'candidates', component: CandidateListComponent },
      { path: 'candidates/:id', component: CandidateDetailComponent },
      { path: 'workflow', component: ApprovalWorkflowComponent }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ProductDiscoveryRoutingModule { }
