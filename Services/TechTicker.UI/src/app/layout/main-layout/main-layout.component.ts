import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { NzModalService } from 'ng-zorro-antd/modal';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Subject, takeUntil } from 'rxjs';
import { AuthService, User } from '../../core/services/auth.service';

@Component({
  selector: 'app-main-layout',
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.css'],
  standalone: false
})
export class MainLayoutComponent implements OnInit, OnDestroy {
  isCollapsed = false;
  currentUser: User | null = null;
  isAuthenticated = false;
  private destroy$ = new Subject<void>();

  constructor(
    private authService: AuthService,
    private router: Router,
    private modal: NzModalService,
    private message: NzMessageService
  ) { }

  ngOnInit(): void {
    // Subscribe to authentication state
    this.authService.isAuthenticated$
      .pipe(takeUntil(this.destroy$))
      .subscribe(isAuthenticated => {
        this.isAuthenticated = isAuthenticated;
      });

    // Subscribe to current user
    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        this.currentUser = user;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleCollapsed(): void {
    this.isCollapsed = !this.isCollapsed;
  }

  getUserDisplayName(): string {
    if (this.currentUser) {
      return `${this.currentUser.firstName} ${this.currentUser.lastName}`;
    }
    return 'Guest User';
  }

  getUserInitials(): string {
    if (this.currentUser) {
      return `${this.currentUser.firstName.charAt(0)}${this.currentUser.lastName.charAt(0)}`.toUpperCase();
    }
    return 'GU';
  }

  onProfileClick(): void {
    this.router.navigate(['/auth/profile']);
  }

  onLogout(): void {
    this.modal.confirm({
      nzTitle: 'Confirm Logout',
      nzContent: 'Are you sure you want to log out?',
      nzOnOk: () => {
        this.authService.logout();
        this.message.success('You have been logged out successfully');
      }
    });
  }

  hasRole(role: string): boolean {
    return this.authService.hasRole(role);
  }

  hasAnyRole(roles: string[]): boolean {
    return this.authService.hasAnyRole(roles);
  }
}
