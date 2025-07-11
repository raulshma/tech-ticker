import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProxyListComponent } from './proxy-management/proxy-list/proxy-list.component';
import { ProxyFormComponent } from './proxy-management/proxy-form/proxy-form.component';
import { BulkImportComponent } from './proxy-management/bulk-import/bulk-import.component';
import { AiSettingsComponent } from './ai-settings/ai-settings.component';
import { BrowserAutomationTesterComponent } from './browser-automation-tester/browser-automation-tester.component';
import { ProductImageManagementComponent } from './product-image-management/product-image-management.component';

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
  },
  {
    path: 'ai-settings',
    component: AiSettingsComponent,
    data: { title: 'AI Configuration' }
  },
  {
    path: 'browser-automation-tester',
    component: BrowserAutomationTesterComponent,
    data: { title: 'Browser Automation Tester' }
  },
  {
    path: 'image-management',
    component: ProductImageManagementComponent,
    data: { title: 'Product Image Management' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule { }
