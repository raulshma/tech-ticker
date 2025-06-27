import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

// Angular Material
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBarModule } from '@angular/material/snack-bar';

// Chart.js - will be added later for price history charts
// import { BaseChartDirective } from 'ng2-charts';

// Routing
import { CatalogRoutingModule } from './catalog-routing.module';

// Shared
import { SharedModule } from '../../shared/shared.module';

// Components
import { CatalogComponent } from './components/catalog/catalog.component';
import { ProductCardComponent } from './components/product-card/product-card.component';
import { ProductDetailComponent } from './components/product-detail/product-detail.component';
import { CategoryFilterComponent } from './components/category-filter/category-filter.component';

@NgModule({
  declarations: [
    CatalogComponent,
    ProductCardComponent,
    ProductDetailComponent,
    CategoryFilterComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CatalogRoutingModule,
    SharedModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatBadgeModule,
    MatDividerModule,
    MatTabsModule,
    MatTooltipModule,
    MatSnackBarModule
    // BaseChartDirective - will be added later for price history charts
  ]
})
export class CatalogModule { }
