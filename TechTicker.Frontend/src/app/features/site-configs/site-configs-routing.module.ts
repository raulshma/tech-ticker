import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SiteConfigsListComponent } from './components/site-configs-list/site-configs-list.component';
import { SiteConfigFormComponent } from './components/site-config-form/site-config-form.component';

const routes: Routes = [
  { path: '', component: SiteConfigsListComponent },
  { path: 'new', component: SiteConfigFormComponent },
  { path: 'edit/:id', component: SiteConfigFormComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SiteConfigsRoutingModule { }
