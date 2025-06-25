import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';

@Component({
  selector: 'app-alerts-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatToolbarModule
  ],
  template: `
    <div class="alerts-container">
      <mat-toolbar>
        <span>Alert Rules</span>
        <span class="spacer"></span>
        <button mat-raised-button color="primary">
          <mat-icon>add</mat-icon>
          Create Alert
        </button>
      </mat-toolbar>

      <mat-card class="alerts-card">
        <mat-card-header>
          <mat-card-title>Your Alert Rules</mat-card-title>
          <mat-card-subtitle>Manage your price and availability alerts</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <div class="empty-state">
            <mat-icon class="empty-icon">notifications_none</mat-icon>
            <h3>No Alert Rules Yet</h3>
            <p>Create your first alert rule to get notified about price changes and product availability.</p>
            <button mat-raised-button color="primary">
              <mat-icon>add</mat-icon>
              Create Your First Alert
            </button>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .alerts-container {
      padding: 20px;
      max-width: 1200px;
      margin: 0 auto;
    }

    .alerts-card {
      margin-top: 20px;
    }

    .spacer {
      flex: 1 1 auto;
    }

    .empty-state {
      text-align: center;
      padding: 60px 20px;
    }

    .empty-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #ccc;
      margin-bottom: 16px;
    }

    .empty-state h3 {
      margin: 16px 0 8px 0;
      color: #666;
    }

    .empty-state p {
      color: #999;
      margin-bottom: 24px;
      max-width: 400px;
      margin-left: auto;
      margin-right: auto;
    }
  `]
})
export class AlertsListComponent implements OnInit {

  constructor() { }

  ngOnInit(): void {
    // Component initialization
  }
}
