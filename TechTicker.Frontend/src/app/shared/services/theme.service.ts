import { Injectable, signal, effect, DOCUMENT } from '@angular/core';
import { inject } from '@angular/core';

export type ThemeMode = 'light' | 'dark' | 'auto';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly storageKey = 'tech-ticker-theme';

  // Signal for the current theme mode
  private readonly _themeMode = signal<ThemeMode>(this.getInitialTheme());
  
  // Computed signal for the actual applied theme (resolves 'auto' to 'light' or 'dark')
  readonly currentTheme = signal<'light' | 'dark'>('light');
  
  // Public readonly access to theme mode
  readonly themeMode = this._themeMode.asReadonly();

  constructor() {
    // Effect to apply theme changes to the DOM
    effect(() => {
      this.applyTheme(this._themeMode());
    });

    // Listen for system theme changes when in auto mode
    this.setupSystemThemeListener();
  }

  /**
   * Set the theme mode
   */
  setTheme(mode: ThemeMode): void {
    this._themeMode.set(mode);
    localStorage.setItem(this.storageKey, mode);
  }

  /**
   * Toggle between light and dark themes
   */
  toggleTheme(): void {
    const current = this._themeMode();
    if (current === 'auto') {
      // If in auto mode, switch to the opposite of current system preference
      const systemPrefersDark = this.getSystemThemePreference();
      this.setTheme(systemPrefersDark ? 'light' : 'dark');
    } else {
      this.setTheme(current === 'light' ? 'dark' : 'light');
    }
  }

  /**
   * Check if the current theme is dark
   */
  isDarkTheme(): boolean {
    return this.currentTheme() === 'dark';
  }

  /**
   * Get the initial theme from localStorage or system preference
   */
  private getInitialTheme(): ThemeMode {
    if (typeof localStorage !== 'undefined') {
      const stored = localStorage.getItem(this.storageKey) as ThemeMode;
      if (stored && ['light', 'dark', 'auto'].includes(stored)) {
        return stored;
      }
    }
    return 'auto'; // Default to auto mode
  }

  /**
   * Apply the theme to the DOM
   */
  private applyTheme(mode: ThemeMode): void {
    const body = this.document.body;
    const isDark = this.resolveThemeMode(mode);
    
    // Update the computed signal
    this.currentTheme.set(isDark ? 'dark' : 'light');
    
    // Apply CSS classes
    body.classList.toggle('dark-theme', isDark);
    
    // Update the color-scheme for better browser integration
    this.document.documentElement.style.colorScheme = isDark ? 'dark' : 'light';
  }

  /**
   * Resolve the theme mode to actual light/dark preference
   */
  private resolveThemeMode(mode: ThemeMode): boolean {
    switch (mode) {
      case 'dark':
        return true;
      case 'light':
        return false;
      case 'auto':
        return this.getSystemThemePreference();
      default:
        return false;
    }
  }

  /**
   * Get system theme preference
   */
  private getSystemThemePreference(): boolean {
    if (typeof window !== 'undefined' && window.matchMedia) {
      return window.matchMedia('(prefers-color-scheme: dark)').matches;
    }
    return false;
  }

  /**
   * Setup listener for system theme changes
   */
  private setupSystemThemeListener(): void {
    if (typeof window !== 'undefined' && window.matchMedia) {
      const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
      
      // Listen for changes
      mediaQuery.addEventListener('change', () => {
        if (this._themeMode() === 'auto') {
          this.applyTheme('auto');
        }
      });
    }
  }
} 