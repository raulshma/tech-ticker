import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { MatSnackBarModule } from '@angular/material/snack-bar';

// Directives
import { HasRoleDirective } from '../directives/has-role.directive';
import { HasAnyRoleDirective } from '../directives/has-any-role.directive';
import { HasAllRolesDirective } from '../directives/has-all-roles.directive';
import { HasPermissionDirective } from '../directives/has-permission.directive';

// Guards
import { AuthGuard } from '../guards/auth.guard';
import { AdminGuard } from '../guards/admin.guard';
import { RoleGuard } from '../guards/role.guard';
import { UserGuard } from '../guards/user.guard';

// Services
import { AuthService } from '../services/auth.service';
import { RoleService } from '../services/role.service';

// Interceptors
import { AuthorizationInterceptor } from '../interceptors/authorization.interceptor';

@NgModule({
  imports: [
    CommonModule,
    MatSnackBarModule,
    HasRoleDirective,
    HasAnyRoleDirective,
    HasAllRolesDirective,
    HasPermissionDirective
  ],
  exports: [
    HasRoleDirective,
    HasAnyRoleDirective,
    HasAllRolesDirective,
    HasPermissionDirective
  ],
  providers: [
    AuthService,
    RoleService,
    AuthGuard,
    AdminGuard,
    RoleGuard,
    UserGuard,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthorizationInterceptor,
      multi: true
    }
  ]
})
export class RbacModule { }
