import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './shared/guards/auth.guard';
import { AdminGuard } from './shared/guards/admin.guard';

const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  {
    path: 'login',
    loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule)
  },
  // TODO: Uncomment these routes as the modules are implemented
  // {
  //   path: 'dashboard',
  //   loadChildren: () => import('./features/dashboard/dashboard.module').then(m => m.DashboardModule),
  //   canActivate: [AuthGuard]
  // },
  // {
  //   path: 'categories',
  //   loadChildren: () => import('./features/categories/categories.module').then(m => m.CategoriesModule),
  //   canActivate: [AuthGuard, AdminGuard]
  // },
  // {
  //   path: 'products',
  //   loadChildren: () => import('./features/products/products.module').then(m => m.ProductsModule),
  //   canActivate: [AuthGuard, AdminGuard]
  // },
  // {
  //   path: 'mappings',
  //   loadChildren: () => import('./features/mappings/mappings.module').then(m => m.MappingsModule),
  //   canActivate: [AuthGuard, AdminGuard]
  // },
  // {
  //   path: 'site-configs',
  //   loadChildren: () => import('./features/site-configs/site-configs.module').then(m => m.SiteConfigsModule),
  //   canActivate: [AuthGuard, AdminGuard]
  // },
  // {
  //   path: 'users',
  //   loadChildren: () => import('./features/users/users.module').then(m => m.UsersModule),
  //   canActivate: [AuthGuard, AdminGuard]
  // },
  // {
  //   path: 'alerts',
  //   loadChildren: () => import('./features/alerts/alerts.module').then(m => m.AlertsModule),
  //   canActivate: [AuthGuard]
  // },
  { path: '**', redirectTo: '/login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
