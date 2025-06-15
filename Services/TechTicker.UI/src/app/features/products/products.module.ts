import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

// NG-ZORRO imports
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzPageHeaderModule } from 'ng-zorro-antd/page-header';
import { NzMessageModule } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NzDescriptionsModule } from 'ng-zorro-antd/descriptions';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzStatisticModule } from 'ng-zorro-antd/statistic';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzGridModule } from 'ng-zorro-antd/grid';

// Guards
import { AdminGuard } from '../../core/guards/admin.guard';

// Components
import { ProductListComponent } from './components/product-list/product-list.component';
import { ProductDetailsComponent } from './components/product-details/product-details.component';
import { ProductFormComponent } from './components/product-form/product-form.component';
import { CategoryListComponent } from './components/category-list/category-list.component';
import { CategoryDetailsComponent } from './components/category-details/category-details.component';
import { CategoryFormComponent } from './components/category-form/category-form.component';
import { PriceHistoryComponent } from './components/price-history/price-history.component';

const routes: Routes = [
  {
    path: '',
    component: ProductListComponent,
    canActivate: [AdminGuard]
  },
  {
    path: 'list',
    component: ProductListComponent,
    canActivate: [AdminGuard]
  },
  {
    path: 'create',
    component: ProductFormComponent,
    canActivate: [AdminGuard]
  },
  {
    path: 'categories',
    children: [
      {
        path: '',
        component: CategoryListComponent,
        canActivate: [AdminGuard]
      },
      {
        path: 'create',
        component: CategoryFormComponent,
        canActivate: [AdminGuard]
      },
      {
        path: ':id',
        component: CategoryDetailsComponent,
        canActivate: [AdminGuard]
      },
      {
        path: ':id/edit',
        component: CategoryFormComponent,
        canActivate: [AdminGuard]
      }
    ]
  },
  {
    path: ':id',
    component: ProductDetailsComponent,
    canActivate: [AdminGuard]
  },
  {
    path: ':id/edit',
    component: ProductFormComponent,
    canActivate: [AdminGuard]
  }
];

@NgModule({
  declarations: [
    ProductListComponent,
    ProductDetailsComponent,
    CategoryListComponent,
    CategoryDetailsComponent,
    ProductFormComponent,
    CategoryFormComponent,
    PriceHistoryComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    RouterModule.forChild(routes),

    // NG-ZORRO modules
    NzTableModule,
    NzButtonModule,
    NzInputModule,
    NzFormModule,
    NzSelectModule,
    NzCardModule,
    NzPageHeaderModule,
    NzMessageModule,
    NzModalModule,
    NzSpinModule,
    NzTagModule,
    NzBadgeModule,
    NzEmptyModule,
    NzIconModule,
    NzToolTipModule,
    NzDescriptionsModule,
    NzSwitchModule,
    NzAlertModule,
    NzStatisticModule,
    NzDatePickerModule,
    NzGridModule
  ]
})
export class ProductsModule { }
