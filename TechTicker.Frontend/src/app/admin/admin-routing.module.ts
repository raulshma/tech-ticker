import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProxyListComponent } from './proxy-management/proxy-list/proxy-list.component';

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
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule { }
