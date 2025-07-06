import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { IntegrationsFeaturesOverviewComponent } from './pages/integrations-features-overview/integrations-features-overview.component';

const routes: Routes = [
  {
    path: '',
    component: IntegrationsFeaturesOverviewComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class IntegrationsFeaturesRoutingModule { } 