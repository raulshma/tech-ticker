import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';

import { AlertFormComponent } from '../../components/alert-form/alert-form.component';
import { AlertRuleDto } from '../../../../shared/api/api-client';

@Component({
  selector: 'app-alert-create',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    AlertFormComponent
  ],
  template: `
    <div class="create-alert-container">
      <app-alert-form
        [productId]="productId"
        (alertSaved)="onAlertSaved($event)"
        (cancelled)="onCancel()">
      </app-alert-form>
    </div>
  `,
  styles: [`
    .create-alert-container {
      padding: 20px;
      max-width: 800px;
      margin: 0 auto;
    }
  `]
})
export class AlertCreateComponent {
  productId?: string;

  constructor(
    private router: Router,
    private route: ActivatedRoute
  ) {
    // Check if we have a productId from query params
    this.productId = this.route.snapshot.queryParams['productId'];
  }

  onAlertSaved(alert: AlertRuleDto): void {
    this.router.navigate(['/alerts']);
  }

  onCancel(): void {
    this.router.navigate(['/alerts']);
  }
}
