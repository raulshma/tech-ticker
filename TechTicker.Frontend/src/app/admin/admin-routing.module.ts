import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProxyListComponent } from './proxy-management/proxy-list/proxy-list.component';
import { ProxyFormComponent } from './proxy-management/proxy-form/proxy-form.component';
import { BulkImportComponent } from './proxy-management/bulk-import/bulk-import.component';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'proxies',
    pathMatch: 'full'
  },
  {
    path: 'proxies',
    component: ProxyListComponent,
    data: { title: 'Proxy Management' }
  },
  {
    path: 'proxies/add',
    component: ProxyFormComponent,
    data: { title: 'Add Proxy' }
  },
  {
    path: 'proxies/edit/:id',
    component: ProxyFormComponent,
    data: { title: 'Edit Proxy' }
  },
  {
    path: 'proxies/bulk-import',
    component: BulkImportComponent,
    data: { title: 'Bulk Import Proxies' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule { }
