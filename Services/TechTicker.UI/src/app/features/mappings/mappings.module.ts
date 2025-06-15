import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

// ng-zorro imports
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzMessageModule } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzDescriptionsModule } from 'ng-zorro-antd/descriptions';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzTabsModule } from 'ng-zorro-antd/tabs';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzStatisticModule } from 'ng-zorro-antd/statistic';
import { NzAutocompleteModule } from 'ng-zorro-antd/auto-complete';

// Guards
import { AdminGuard } from '../../core/guards/admin.guard';

// Components
import { SiteConfigListComponent } from './components/site-config-list/site-config-list.component';
import { SiteConfigFormComponent } from './components/site-config-form/site-config-form.component';
import { MappingListComponent } from './components/mapping-list/mapping-list.component';

// Services are already created as standalone

const routes: Routes = [
  {
    path: '',
    redirectTo: 'site-configs',
    pathMatch: 'full'
  },
  {
    path: 'site-configs',
    component: SiteConfigListComponent,
    canActivate: [AdminGuard],
    data: { title: 'Site Configurations' }
  },
  {
    path: 'site-configs/create',
    component: SiteConfigFormComponent,
    canActivate: [AdminGuard],
    data: { title: 'Create Site Configuration' }
  },
  {
    path: 'site-configs/:id',
    component: SiteConfigFormComponent,
    canActivate: [AdminGuard],
    data: { title: 'View Site Configuration' }
  },
  {
    path: 'site-configs/:id/edit',
    component: SiteConfigFormComponent,
    canActivate: [AdminGuard],
    data: { title: 'Edit Site Configuration' }
  },
  {
    path: 'product-seller',
    component: MappingListComponent,
    canActivate: [AdminGuard],
    data: { title: 'Product-Seller Mappings' }
  }
];

@NgModule({
  declarations: [
    SiteConfigListComponent,
    SiteConfigFormComponent,
    MappingListComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    RouterModule.forChild(routes),

    // ng-zorro modules
    NzTableModule,
    NzButtonModule,
    NzInputModule,
    NzSelectModule,
    NzFormModule,
    NzCardModule,
    NzTagModule,
    NzMessageModule,
    NzModalModule,
    NzIconModule,
    NzGridModule,
    NzEmptyModule,
    NzToolTipModule,
    NzCheckboxModule,
    NzAlertModule,
    NzDescriptionsModule,
    NzSpinModule,
    NzDividerModule,
    NzTabsModule,
    NzBadgeModule,
    NzStatisticModule,
    NzAutocompleteModule
  ]
})
export class MappingsModule { }
