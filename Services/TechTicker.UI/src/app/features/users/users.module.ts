import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

// ng-zorro imports
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzMessageModule } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NzBreadCrumbModule } from 'ng-zorro-antd/breadcrumb';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzResultModule } from 'ng-zorro-antd/result';
import { NzCollapseModule } from 'ng-zorro-antd/collapse';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzProgressModule } from 'ng-zorro-antd/progress';

// Guards
import { AdminGuard } from '../../core/guards/admin.guard';

// Components
import { UserListComponent } from './components/user-list/user-list.component';
import { UserDetailsComponent } from './components/user-details/user-details.component';
import { UserEditComponent } from './components/user-edit/user-edit.component';
import { UserRolesComponent } from './components/user-roles/user-roles.component';
import { UserCreateComponent } from './components/user-create/user-create.component';

const routes: Routes = [
  {
    path: '',
    component: UserListComponent,
    canActivate: [AdminGuard],
    data: { title: 'Users' }
  },
  {
    path: 'create',
    component: UserCreateComponent,
    canActivate: [AdminGuard],
    data: { title: 'Create User' }
  },
  {
    path: 'roles',
    component: UserRolesComponent,
    canActivate: [AdminGuard],
    data: { title: 'User Roles Management' }
  },
  {
    path: ':id',
    component: UserDetailsComponent,
    canActivate: [AdminGuard],
    data: { title: 'User Details' }
  },
  {
    path: ':id/edit',
    component: UserEditComponent,
    canActivate: [AdminGuard],
    data: { title: 'Edit User' }
  },
  {
    path: ':id/roles',
    component: UserRolesComponent,
    canActivate: [AdminGuard],
    data: { title: 'Manage User Roles' }
  }
];

@NgModule({
  declarations: [],
  imports: [
    UserListComponent,
    UserDetailsComponent,
    UserEditComponent,
    UserRolesComponent,
    UserCreateComponent,
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    RouterModule.forChild(routes),

    // ng-zorro modules
    NzCardModule,
    NzTableModule,
    NzButtonModule,
    NzIconModule,
    NzInputModule,
    NzSelectModule,
    NzTagModule,
    NzMessageModule,
    NzModalModule,
    NzSpinModule,
    NzFormModule,
    NzGridModule,
    NzToolTipModule,
    NzBreadCrumbModule,
    NzAvatarModule,
    NzResultModule,
    NzCollapseModule,
    NzAlertModule,
    NzEmptyModule,
    NzProgressModule
  ],
  providers: []
})
export class UsersModule { }
