import { Directive, Input, TemplateRef, ViewContainerRef, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { RoleService } from '../services/role.service';

@Directive({
  selector: '[hasPermission]',
  standalone: true
})
export class HasPermissionDirective implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private hasView = false;

  @Input() set hasPermission(permission: string | string[]) {
    this.requiredPermissions = Array.isArray(permission) ? permission : [permission];
    this.updateView();
  }

  @Input() set hasPermissionRequireAll(requireAll: boolean) {
    this.requireAll = requireAll;
    this.updateView();
  }

  private requiredPermissions: string[] = [];
  private requireAll = false;

  constructor(
    private templateRef: TemplateRef<any>,
    private viewContainer: ViewContainerRef,
    private authService: AuthService,
    private roleService: RoleService
  ) {}

  ngOnInit(): void {
    // Listen to user changes
    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.updateView();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private updateView(): void {
    let hasAccess = false;

    if (this.requiredPermissions.length === 0) {
      hasAccess = true;
    } else if (this.requireAll) {
      hasAccess = this.roleService.hasAllPermissions(this.requiredPermissions);
    } else {
      hasAccess = this.roleService.hasAnyPermission(this.requiredPermissions);
    }

    if (hasAccess && !this.hasView) {
      this.viewContainer.createEmbeddedView(this.templateRef);
      this.hasView = true;
    } else if (!hasAccess && this.hasView) {
      this.viewContainer.clear();
      this.hasView = false;
    }
  }
}
