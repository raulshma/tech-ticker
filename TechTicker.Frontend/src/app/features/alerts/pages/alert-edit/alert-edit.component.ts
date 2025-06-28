import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { AlertFormComponent } from '../../components/alert-form/alert-form.component';
import { AlertsService } from '../../services/alerts.service';
import { AlertRuleDto } from '../../../../shared/api/api-client';

@Component({
  selector: 'app-alert-edit',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    AlertFormComponent
  ],
  template: `
    <div class="edit-alert-container">
      <div class="loading-container" *ngIf="isLoading">
        <mat-spinner></mat-spinner>
        <p>Loading alert details...</p>
      </div>

      <app-alert-form
        *ngIf="!isLoading && alertRule"
        [alertRule]="alertRule"
        (alertSaved)="onAlertSaved($event)"
        (cancelled)="onCancel()">
      </app-alert-form>

      <mat-card *ngIf="!isLoading && !alertRule" class="error-card">
        <mat-card-content>
          <h3>Alert Not Found</h3>
          <p>The alert rule you're trying to edit could not be found.</p>
          <button mat-raised-button color="primary" (click)="onCancel()">
            Back to Alerts
          </button>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .edit-alert-container {
      padding: 20px;
      max-width: 800px;
      margin: 0 auto;
    }

    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 40px;
    }

    .loading-container p {
      margin-top: 16px;
      color: #666;
    }

    .error-card {
      text-align: center;
      padding: 40px;
    }

    .error-card h3 {
      margin-bottom: 16px;
      color: #666;
    }

    .error-card p {
      margin-bottom: 24px;
      color: #999;
    }
  `]
})
export class AlertEditComponent implements OnInit {
  alertRule?: AlertRuleDto;
  isLoading = true;
  alertRuleId!: string;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private alertsService: AlertsService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.alertRuleId = this.route.snapshot.params['id'];
    this.loadAlertRule();
  }

  private loadAlertRule(): void {
    // Since we don't have a direct getById method in the service,
    // we'll load all user alerts and find the one we need
    this.alertsService.getUserAlerts().subscribe({
      next: (alerts) => {
        this.alertRule = alerts.find(alert => alert.alertRuleId === this.alertRuleId);
        this.isLoading = false;
        
        if (!this.alertRule) {
          this.snackBar.open('Alert rule not found', 'Close', { duration: 3000 });
        }
      },
      error: (error) => {
        console.error('Error loading alert rule:', error);
        this.snackBar.open('Failed to load alert rule', 'Close', { duration: 3000 });
        this.isLoading = false;
      }
    });
  }

  onAlertSaved(alert: AlertRuleDto): void {
    this.router.navigate(['/alerts']);
  }

  onCancel(): void {
    this.router.navigate(['/alerts']);
  }
}
