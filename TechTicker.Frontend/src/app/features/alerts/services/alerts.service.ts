import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

export interface AlertRule {
  id: string;
  name: string;
  productId: string;
  productName: string;
  alertType: 'price_drop' | 'price_increase' | 'availability' | 'back_in_stock';
  threshold?: number;
  isActive: boolean;
  createdAt: Date;
  lastTriggered?: Date;
}

@Injectable({
  providedIn: 'root'
})
export class AlertsService {

  constructor() { }

  // Placeholder methods for future implementation
  getAlertRules(): Observable<AlertRule[]> {
    // Return empty array for now - will be implemented when backend is ready
    return of([]);
  }

  createAlertRule(alertRule: Partial<AlertRule>): Observable<AlertRule> {
    // Placeholder implementation
    const newAlert: AlertRule = {
      id: Math.random().toString(36).substr(2, 9),
      name: alertRule.name || '',
      productId: alertRule.productId || '',
      productName: alertRule.productName || '',
      alertType: alertRule.alertType || 'price_drop',
      threshold: alertRule.threshold,
      isActive: true,
      createdAt: new Date()
    };
    
    return of(newAlert);
  }

  updateAlertRule(id: string, updates: Partial<AlertRule>): Observable<AlertRule> {
    // Placeholder implementation
    return of({} as AlertRule);
  }

  deleteAlertRule(id: string): Observable<void> {
    // Placeholder implementation
    return of(void 0);
  }

  toggleAlertRule(id: string): Observable<AlertRule> {
    // Placeholder implementation
    return of({} as AlertRule);
  }
}
