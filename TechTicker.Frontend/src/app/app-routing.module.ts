import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './shared/guards/auth.guard';
import { AdminGuard } from './shared/guards/admin.guard';
import { RoleGuard } from './shared/guards/role.guard';
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
        path: 'catalog',
        loadChildren: () => import('./features/catalog/catalog.module').then(m => m.CatalogModule)
      },
      {
        path: 'categories',
        loadChildren: () => import('./features/categories/categories.module').then(m => m.CategoriesModule),
        canActivate: [RoleGuard],
        data: { roles: ['Admin', 'Moderator'] }
      },
      {
        path: 'products',
        loadChildren: () => import('./features/products/products.module').then(m => m.ProductsModule),
        canActivate: [RoleGuard],
        data: { roles: ['Admin', 'Moderator'] }
      },
      {
        path: 'product-comparison',
        loadChildren: () => import('./features/product-comparison/product-comparison.module').then(m => m.ProductComparisonModule),
        canActivate: [RoleGuard],
        data: { roles: ['User', 'Admin', 'Moderator'] } // Accessible to Users, Admins, and Moderators
      },
      {
        path: 'mappings',
        loadChildren: () => import('./features/mappings/mappings.module').then(m => m.MappingsModule),
        canActivate: [RoleGuard],
        data: { roles: ['User', 'Admin', 'Moderator'] } // Accessible to Users, Admins, and Moderators
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
      },
      {
        path: 'scraper-logs',
        loadChildren: () => import('./features/scraper-logs/scraper-logs.module').then(m => m.ScraperLogsModule),
        canActivate: [AdminGuard]
      },
      {
        path: 'alerts',
        loadChildren: () => import('./features/alerts/alerts.module').then(m => m.AlertsModule),
        canActivate: [RoleGuard],
        data: { roles: ['User', 'Admin'] } // Accessible to Users and Admins
      },
      {
        path: 'notification-settings',
        loadChildren: () => import('./features/notification-settings/notification-settings.module').then(m => m.NotificationSettingsModule),
        canActivate: [RoleGuard],
        data: { roles: ['User', 'Admin'] } // Accessible to Users and Admins
      },
      {
        path: 'rbac-demo',
        loadComponent: () => import('./shared/components/rbac-demo/rbac-demo.component').then(c => c.RbacDemoComponent),
        canActivate: [AuthGuard] // Available to all authenticated users for testing
      },
      {
        path: 'ai-demo',
        loadComponent: () => import('./features/ai-demo/ai-demo.component').then(c => c.AiDemoComponent),
        canActivate: [AuthGuard] // Available to all authenticated users
      },
      {
        path: 'admin',
        loadChildren: () => import('./admin/admin.module').then(m => m.AdminModule),
        canActivate: [AdminGuard]
      }
    ]
  },
  { path: '**', redirectTo: '/login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
