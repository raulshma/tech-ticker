import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './shared/guards/auth.guard';
import { AdminGuard } from './shared/guards/admin.guard';
import { AppLayoutComponent } from './shared/components/layout/app-layout.component';

const routes: Routes = [
  {
    path: 'login',
    loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule)
  },
  {
    path: '',
    component: AppLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.module').then(m => m.DashboardModule)
      },
      {
        path: 'categories',
        loadChildren: () => import('./features/categories/categories.module').then(m => m.CategoriesModule),
        canActivate: [AdminGuard]
      },
      {
        path: 'products',
        loadChildren: () => import('./features/products/products.module').then(m => m.ProductsModule),
        canActivate: [AdminGuard]
      },
      {
        path: 'mappings',
        loadChildren: () => import('./features/mappings/mappings.module').then(m => m.MappingsModule),
        canActivate: [AdminGuard]
      },
      {
        path: 'site-configs',
        loadChildren: () => import('./features/site-configs/site-configs.module').then(m => m.SiteConfigsModule),
        canActivate: [AdminGuard]
      },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.module').then(m => m.UsersModule),
        canActivate: [AdminGuard]
      }
      // {
      //   path: 'alerts',
      //   loadChildren: () => import('./features/alerts/alerts.module').then(m => m.AlertsModule)
      // }
    ]
  },
  { path: '**', redirectTo: '/login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
