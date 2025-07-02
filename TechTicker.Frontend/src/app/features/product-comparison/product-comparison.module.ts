import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { MatTabsModule } from '@angular/material/tabs';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { BaseChartDirective } from 'ng2-charts';

import { ProductComparisonRoutingModule } from './product-comparison-routing.module';
import { SharedModule } from '../../shared/shared.module';
import { ProductComparisonViewComponent } from './components/product-comparison-view/product-comparison-view.component';
import { ProductSelectorComponent } from './components/product-selector/product-selector.component';
import { SpecificationComparisonComponent } from './components/specification-comparison/specification-comparison.component';
import { PriceAnalysisComponent } from './components/price-analysis/price-analysis.component';
import { RecommendationDisplayComponent } from './components/recommendation-display/recommendation-display.component';
import { ProductComparisonSummaryComponent } from './components/product-comparison-summary/product-comparison-summary.component';

@NgModule({
  declarations: [
    ProductComparisonViewComponent,
    ProductSelectorComponent,
    SpecificationComparisonComponent,
    PriceAnalysisComponent,
    RecommendationDisplayComponent,
    ProductComparisonSummaryComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ProductComparisonRoutingModule,
    SharedModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCardModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatChipsModule,
    MatSlideToggleModule,
    MatButtonToggleModule,
    MatExpansionModule,
    MatBadgeModule,
    MatDividerModule,
    MatTabsModule,
    MatAutocompleteModule,
    BaseChartDirective
  ]
})
export class ProductComparisonModule { }