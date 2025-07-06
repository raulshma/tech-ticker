import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

// Angular Material
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDividerModule } from '@angular/material/divider';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatListModule } from '@angular/material/list';
import { MatGridListModule } from '@angular/material/grid-list';

// Routing
import { IntegrationsFeaturesRoutingModule } from './integrations-features-routing.module';

// Components
import { IntegrationsFeaturesOverviewComponent } from './pages/integrations-features-overview/integrations-features-overview.component';

// Services
import { IntegrationsFeaturesService } from './services/integrations-features.service';

@NgModule({
  declarations: [
    IntegrationsFeaturesOverviewComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    IntegrationsFeaturesRoutingModule,
    
    // Angular Material
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatChipsModule,
    MatBadgeModule,
    MatTooltipModule,
    MatExpansionModule,
    MatTabsModule,
    MatDividerModule,
    MatMenuModule,
    MatDialogModule,
    MatSnackBarModule,
    MatListModule,
    MatGridListModule
  ],
  providers: [
    IntegrationsFeaturesService
  ]
})
export class IntegrationsFeaturesModule { } 