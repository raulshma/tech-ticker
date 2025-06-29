import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import {
  AuthService,
  CurrentUser,
} from '../../services/auth.service';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-layout',
  templateUrl: './app-layout.component.html',
  styleUrls: ['./app-layout.component.scss'],
  standalone: false,
})
export class AppLayoutComponent implements OnInit, OnDestroy {
  currentUser: CurrentUser | null = null;
  private destroy$ = new Subject<void>();
  
  // Track expanded sections for collapsible navigation
  expandedSections: { [key: string]: boolean } = {
    productManagement: true,
    infrastructure: false,
    monitoring: false,
    userManagement: false,
    contentManagement: true,
    personal: true
  };

  constructor(
    private authService: AuthService, 
    private router: Router,
    public themeService: ThemeService
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe((user) => {
        this.currentUser = user;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  navigateTo(route: string): void {
    this.router.navigate([route]);
  }

  toggleSection(sectionKey: string): void {
    this.expandedSections[sectionKey] = !this.expandedSections[sectionKey];
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  get isDarkTheme(): boolean {
    return this.themeService.isDarkTheme();
  }

  get themeIcon(): string {
    switch (this.themeService.themeMode()) {
      case 'dark':
        return 'dark_mode';
      case 'light':
        return 'light_mode';
      case 'auto':
        return this.themeService.isDarkTheme() ? 'brightness_auto' : 'brightness_auto';
      default:
        return 'light_mode';
    }
  }

  get themeTooltip(): string {
    switch (this.themeService.themeMode()) {
      case 'dark':
        return 'Switch to light mode';
      case 'light':
        return 'Switch to dark mode';
      case 'auto':
        return `Auto mode (currently ${this.themeService.isDarkTheme() ? 'dark' : 'light'})`;
      default:
        return 'Toggle theme';
    }
  }
}
