import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';

import { AlertsListComponent } from './components/alerts-list/alerts-list.component';
import { AlertCreateComponent } from './pages/alert-create/alert-create.component';
import { AlertEditComponent } from './pages/alert-edit/alert-edit.component';
import { AlertPerformanceComponent } from './components/alert-performance/alert-performance.component';
import { AlertAdminComponent } from './pages/alert-admin/alert-admin.component';

const routes: Routes = [
  {
    path: '',
    component: AlertsListComponent
  },
  {
    path: 'create',
    component: AlertCreateComponent
  },
  {
    path: 'edit/:id',
    component: AlertEditComponent
  },
  {
    path: 'admin',
    component: AlertAdminComponent
  },
  {
    path: 'performance',
    component: AlertPerformanceComponent
  }
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes)
  ]
})
export class AlertsModule { }
