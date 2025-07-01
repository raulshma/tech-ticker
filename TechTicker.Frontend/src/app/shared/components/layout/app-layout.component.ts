import { Component, OnInit, OnDestroy, ViewChild, HostListener } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { Subject, takeUntil, filter } from 'rxjs';
import { MatSidenav } from '@angular/material/sidenav';
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
  @ViewChild('drawer') drawer!: MatSidenav;
  
  currentUser: CurrentUser | null = null;
  private destroy$ = new Subject<void>();
  isMobile: boolean = false;
  isSidebarOpen: boolean = true; // Track sidebar state for desktop
  
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
  ) {
    this.checkScreenSize();
  }

  ngOnInit(): void {
    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe((user) => {
        this.currentUser = user;
      });
    
    // Load sidebar state from localStorage (desktop only)
    if (!this.isMobile) {
      const savedState = localStorage.getItem('sidebarOpen');
      if (savedState !== null) {
        this.isSidebarOpen = savedState === 'true';
      }
    }
    
    // Close sidebar on mobile when navigating
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.closeSidenavOnMobile();
      });
    
    // Initialize body overflow state
    if (this.isMobile) {
      document.body.style.overflow = '';
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    // Restore body scroll on component destroy
    document.body.style.overflow = '';
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

  @HostListener('window:resize', ['$event'])
  onResize(event: any): void {
    const wasMobile = this.isMobile;
    this.checkScreenSize();
    
    // Handle transition between mobile and desktop
    if (wasMobile !== this.isMobile) {
      // Reset body overflow when switching modes
      document.body.style.overflow = '';
      
      // Close sidebar when switching to mobile
      if (this.isMobile && this.drawer) {
        this.drawer.close();
      }
      
      // Reset sidebar state when switching to desktop
      if (!this.isMobile) {
        this.isSidebarOpen = true;
      }
    }
  }

  private checkScreenSize(): void {
    this.isMobile = window.innerWidth < 768;
  }

  toggleSidenav(): void {
    if (this.drawer) {
      if (this.isMobile) {
        // On mobile, just toggle the drawer
        this.drawer.toggle();
      } else {
        // On desktop, track the state and toggle
        this.isSidebarOpen = !this.isSidebarOpen;
        // Save state to localStorage
        localStorage.setItem('sidebarOpen', this.isSidebarOpen.toString());
        if (this.isSidebarOpen) {
          this.drawer.open();
        } else {
          this.drawer.close();
        }
      }
    }
  }

  closeSidenavOnMobile(): void {
    if (this.isMobile && this.drawer) {
      this.drawer.close();
      // Re-enable body scroll
      document.body.style.overflow = '';
    }
  }

  onSidenavStateChange(isOpen: boolean): void {
    // Handle body scroll for mobile
    if (this.isMobile) {
      if (isOpen) {
        document.body.style.overflow = 'hidden';
      } else {
        document.body.style.overflow = '';
      }
    } else {
      // Update desktop sidebar state
      this.isSidebarOpen = isOpen;
      // Save state to localStorage
      localStorage.setItem('sidebarOpen', this.isSidebarOpen.toString());
    }
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
